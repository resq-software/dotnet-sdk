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

using Microsoft.Extensions.Options;
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;

namespace ResQ.Mavlink.Gateway.Gcs;

/// <summary>
/// Bridges a GCS (Ground Control Station) transport to the vehicle transport, with optional
/// ResQ priority override that suppresses GCS command packets shortly after ResQ sends a command.
/// </summary>
/// <remarks>
/// <para>
/// When started, two background tasks run concurrently:
/// <list type="bullet">
/// <item><description>GCS → Vehicle: packets received from the GCS are forwarded to the vehicle transport.</description></item>
/// <item><description>Vehicle → GCS: packets received from the vehicle are forwarded to the GCS transport.</description></item>
/// </list>
/// </para>
/// <para>
/// When <see cref="GcsPassthroughOptions.ResqPriorityOverride"/> is <see langword="true"/>, GCS command
/// packets (CommandLong = 76, SetMode = 11, SetPositionTargetGlobalInt = 86) are silently dropped
/// for 2 seconds after <see cref="NotifyResqCommand"/> has been called.
/// </para>
/// </remarks>
public sealed class GcsPassthrough : IAsyncDisposable
{
    // MAVLink message IDs considered "command" packets subject to ResQ priority override.
    private static readonly HashSet<uint> CommandMessageIds = [76u, 11u, 86u];

    private const double ResqCommandWindowSeconds = 2.0;

    private readonly IMavlinkTransport _vehicleTransport;
    private readonly GcsPassthroughOptions _options;
    private readonly IMavlinkTransport _gcsTransport;
    private readonly CancellationTokenSource _cts = new();

    private DateTimeOffset _lastResqCommandTime = DateTimeOffset.MinValue;
    private Task? _gcsToVehicleTask;
    private Task? _vehicleToGcsTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="GcsPassthrough"/> using DI-resolved options.
    /// </summary>
    /// <param name="vehicleTransport">The transport connected to the vehicle.</param>
    /// <param name="options">GCS passthrough configuration wrapped in IOptions.</param>
    public GcsPassthrough(IMavlinkTransport vehicleTransport, IOptions<GcsPassthroughOptions> options)
        : this(vehicleTransport, options.Value)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="GcsPassthrough"/> with raw options.
    /// </summary>
    /// <param name="vehicleTransport">The transport connected to the vehicle.</param>
    /// <param name="options">GCS passthrough configuration.</param>
    public GcsPassthrough(IMavlinkTransport vehicleTransport, GcsPassthroughOptions options)
    {
        _vehicleTransport = vehicleTransport ?? throw new ArgumentNullException(nameof(vehicleTransport));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _gcsTransport = new UdpTransport(new UdpTransportOptions
        {
            ListenPort = _options.GcsListenPort,
            RemoteHost = "127.0.0.1",
            RemotePort = _options.GcsListenPort,
        });
    }

    /// <summary>
    /// Initializes a new <see cref="GcsPassthrough"/> with an externally provided GCS transport (for testing).
    /// </summary>
    /// <param name="vehicleTransport">The transport connected to the vehicle.</param>
    /// <param name="gcsTransport">The transport connected to the GCS (injected for testability).</param>
    /// <param name="options">GCS passthrough configuration.</param>
    public GcsPassthrough(IMavlinkTransport vehicleTransport, IMavlinkTransport gcsTransport, GcsPassthroughOptions options)
    {
        _vehicleTransport = vehicleTransport ?? throw new ArgumentNullException(nameof(vehicleTransport));
        _gcsTransport = gcsTransport ?? throw new ArgumentNullException(nameof(gcsTransport));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Starts the bidirectional forwarding background tasks.
    /// </summary>
    /// <param name="ct">Cancellation token that, when cancelled, will stop forwarding.</param>
    public Task StartAsync(CancellationToken ct = default)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token).Token;
        _gcsToVehicleTask = RunGcsToVehicleAsync(linked);
        _vehicleToGcsTask = RunVehicleToGcsAsync(linked);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Notifies the passthrough that a ResQ command has just been sent.
    /// GCS command packets will be suppressed for the next 2 seconds.
    /// </summary>
    public void NotifyResqCommand()
    {
        _lastResqCommandTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Forwards a raw packet received from the vehicle side to the GCS transport.
    /// </summary>
    /// <param name="packet">The packet to forward.</param>
    /// <param name="ct">Cancellation token.</param>
    public ValueTask ForwardToGcsAsync(MavlinkPacket packet, CancellationToken ct = default)
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        return _gcsTransport.SendAsync(packet, ct);
    }

    private async Task RunGcsToVehicleAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var packet in _gcsTransport.ReceiveAsync(ct).ConfigureAwait(false))
            {
                if (_options.ResqPriorityOverride && IsCommandPacket(packet))
                {
                    var elapsed = (DateTimeOffset.UtcNow - _lastResqCommandTime).TotalSeconds;
                    if (elapsed < ResqCommandWindowSeconds)
                    {
                        // Drop GCS command — ResQ has priority right now.
                        continue;
                    }
                }

                try
                {
                    await _vehicleTransport.SendAsync(packet, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Swallow individual send errors; keep forwarding.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task RunVehicleToGcsAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var packet in _vehicleTransport.ReceiveAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    await _gcsTransport.SendAsync(packet, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Swallow individual send errors; keep forwarding.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private static bool IsCommandPacket(MavlinkPacket packet) =>
        CommandMessageIds.Contains(packet.MessageId);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _cts.CancelAsync().ConfigureAwait(false);

        if (_gcsToVehicleTask is not null)
        {
            try { await _gcsToVehicleTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        if (_vehicleToGcsTask is not null)
        {
            try { await _vehicleToGcsTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        await _gcsTransport.DisposeAsync().ConfigureAwait(false);
        _cts.Dispose();
    }
}
