/**
 * Copyright 2026 ResQ
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using ResQ.Core;
using ResQ.Mavlink.Connection;
using ResQ.Mavlink.Gateway.Gcs;
using ResQ.Mavlink.Gateway.Routing;
using ResQ.Mavlink.Gateway.State;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Transport;

namespace ResQ.Mavlink.Gateway;

/// <summary>
/// Top-level MAVLink gateway orchestrator. Receives raw MAVLink packets from a vehicle
/// transport, deserializes them, updates vehicle state, decides whether to forward telemetry
/// to ResQ, and optionally bridges a GCS connection with priority override.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IMavlinkGateway"/> and therefore <see cref="Microsoft.Extensions.Hosting.IHostedService"/>;
/// register it with <c>services.AddHostedService&lt;MavlinkGateway&gt;()</c> or use the
/// test constructor to inject a fake transport.
/// </para>
/// </remarks>
public sealed class MavlinkGateway : IMavlinkGateway
{
    private readonly IMavlinkTransport _vehicleTransport;
    private readonly MavlinkGatewayOptions _gatewayOptions;
    private readonly GatewayRoutingOptions _routingOptions;
    private readonly GcsPassthroughOptions _gcsOptions;
    private readonly bool _ownsTransport;

    private readonly VehicleStateTracker _stateTracker = new();
    private readonly GatewayRouter _router;
    private readonly Channel<TelemetryPacket> _telemetryChannel;

    private MavlinkConnection? _connection;
    private GcsPassthrough? _gcsPassthrough;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoopTask;
    private bool _disposed;

    // ConnectedSystems: for now a single-connection model; keyed by system ID once first heartbeat seen.
    private readonly Dictionary<byte, MavlinkConnection> _connectedSystems = new();

    /// <summary>
    /// DI constructor — creates a <see cref="UdpTransport"/> internally from
    /// <see cref="MavlinkGatewayOptions.VehicleListenPort"/>.
    /// </summary>
    /// <param name="gatewayOptions">Gateway configuration.</param>
    /// <param name="routingOptions">Routing/rate-limit configuration.</param>
    /// <param name="gcsOptions">GCS passthrough configuration.</param>
    public MavlinkGateway(
        IOptions<MavlinkGatewayOptions> gatewayOptions,
        IOptions<GatewayRoutingOptions> routingOptions,
        IOptions<GcsPassthroughOptions> gcsOptions)
        : this(
            CreateUdpTransport(gatewayOptions.Value.VehicleListenPort),
            gatewayOptions,
            routingOptions,
            gcsOptions,
            ownsTransport: true)
    {
    }

    /// <summary>
    /// Test constructor — accepts an externally provided transport so unit tests can inject fakes.
    /// </summary>
    /// <param name="vehicleTransport">The vehicle-side MAVLink transport.</param>
    /// <param name="gatewayOptions">Gateway configuration.</param>
    /// <param name="routingOptions">Routing/rate-limit configuration.</param>
    /// <param name="gcsOptions">GCS passthrough configuration.</param>
    public MavlinkGateway(
        IMavlinkTransport vehicleTransport,
        IOptions<MavlinkGatewayOptions> gatewayOptions,
        IOptions<GatewayRoutingOptions> routingOptions,
        IOptions<GcsPassthroughOptions> gcsOptions)
        : this(vehicleTransport, gatewayOptions, routingOptions, gcsOptions, ownsTransport: false)
    {
    }

    private MavlinkGateway(
        IMavlinkTransport vehicleTransport,
        IOptions<MavlinkGatewayOptions> gatewayOptions,
        IOptions<GatewayRoutingOptions> routingOptions,
        IOptions<GcsPassthroughOptions> gcsOptions,
        bool ownsTransport)
    {
        _vehicleTransport = vehicleTransport ?? throw new ArgumentNullException(nameof(vehicleTransport));
        _gatewayOptions = gatewayOptions.Value;
        _routingOptions = routingOptions.Value;
        _gcsOptions = gcsOptions.Value;
        _ownsTransport = ownsTransport;

        _router = new GatewayRouter(routingOptions);
        _telemetryChannel = Channel.CreateUnbounded<TelemetryPacket>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<byte, MavlinkConnection> ConnectedSystems => _connectedSystems;

    /// <inheritdoc/>
    public VehicleStateTracker StateTracker => _stateTracker;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();

        var connOpts = new MavlinkConnectionOptions
        {
            SystemId = _gatewayOptions.GatewaySystemId,
            ComponentId = _gatewayOptions.GatewayComponentId,
            HeartbeatInterval = TimeSpan.FromSeconds(1),
        };
        _connection = new MavlinkConnection(_vehicleTransport, connOpts);

        if (_gcsOptions.Enabled)
        {
            _gcsPassthrough = new GcsPassthrough(_vehicleTransport, _gcsOptions);
            _ = _gcsPassthrough.StartAsync(_cts.Token);
        }

        _receiveLoopTask = RunReceiveLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }

        if (_receiveLoopTask is not null)
        {
            try { await _receiveLoopTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
    }

    /// <inheritdoc/>
    public async ValueTask SendToVehicleAsync(byte systemId, IMavlinkMessage msg, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection is null)
            throw new InvalidOperationException("Gateway has not been started.");

        await _connection.SendMessageAsync(msg, ct).ConfigureAwait(false);
        _gcsPassthrough?.NotifyResqCommand();
    }

    /// <inheritdoc/>
    public async ValueTask BroadcastAsync(IMavlinkMessage msg, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection is null)
            throw new InvalidOperationException("Gateway has not been started.");

        await _connection.SendMessageAsync(msg, ct).ConfigureAwait(false);
        _gcsPassthrough?.NotifyResqCommand();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TelemetryPacket> TelemetryFeed(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var packet in _telemetryChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            yield return packet;
        }
    }

    private async Task RunReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var rawPacket in _vehicleTransport.ReceiveAsync(ct).ConfigureAwait(false))
            {
                // Deserialize the message payload.
                if (!MessageRegistry.TryDeserialize(rawPacket.MessageId, rawPacket.Payload.Span, out var message)
                    || message is null)
                {
                    // Unknown message — still forward to GCS passthrough for transparency.
                    if (_gcsPassthrough is not null)
                    {
                        try { await _gcsPassthrough.ForwardToGcsAsync(rawPacket, ct).ConfigureAwait(false); }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MavlinkGateway] Forward to GCS error: {ex.Message}");
                        }
                    }
                    continue;
                }

                var systemId = rawPacket.SystemId;

                // Track connection.
                if (_connection is not null && !_connectedSystems.ContainsKey(systemId))
                {
                    _connectedSystems[systemId] = _connection;
                }

                // Update vehicle state.
                _stateTracker.Update(systemId, message);

                // Route to ResQ if appropriate.
                if (_router.ShouldForwardToResq(systemId, rawPacket.MessageId))
                {
                    var telemetry = _stateTracker.ToTelemetryPacket(systemId);
                    if (telemetry is not null)
                    {
                        _telemetryChannel.Writer.TryWrite(telemetry);
                    }
                    _router.RecordForwarded(systemId);
                }

                // Forward raw packet to GCS.
                if (_gcsPassthrough is not null)
                {
                    try { await _gcsPassthrough.ForwardToGcsAsync(rawPacket, ct).ConfigureAwait(false); }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MavlinkGateway] Forward to GCS error: {ex.Message}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }

        _telemetryChannel.Writer.TryComplete();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }

        if (_receiveLoopTask is not null)
        {
            try { await _receiveLoopTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        if (_gcsPassthrough is not null)
        {
            await _gcsPassthrough.DisposeAsync().ConfigureAwait(false);
        }

        if (_ownsTransport)
        {
            await _vehicleTransport.DisposeAsync().ConfigureAwait(false);
        }

        _cts?.Dispose();
    }

    private static UdpTransport CreateUdpTransport(int listenPort) =>
        new(new UdpTransportOptions
        {
            ListenPort = listenPort,
            RemoteHost = "127.0.0.1",
            RemotePort = listenPort,
        });
}
