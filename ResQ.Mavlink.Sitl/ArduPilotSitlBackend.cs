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

using System.Numerics;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Connection;
using ResQ.Mavlink.Enums;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl;

/// <summary>
/// <see cref="IFlightBackend"/> implementation that drives a live ArduPilot SITL process,
/// communicating over MAVLink UDP and reading telemetry to produce <see cref="DronePhysicsState"/>.
/// </summary>
/// <remarks>
/// This class requires an ArduPilot SITL binary to be present on the system. It is not
/// suitable for unit testing — use <see cref="FlightModelBackendAdapter"/> for offline testing.
/// </remarks>
public sealed class ArduPilotSitlBackend : IFlightBackend
{
    private readonly SitlBackendOptions _options;
    private readonly SitlProcessManager _processManager;

    private MavlinkConnection? _connection;
    private UdpTransport? _transport;

    // Latest telemetry snapshots protected by a simple lock.
    private readonly object _telemetryLock = new();
    private GlobalPositionInt _lastPosition;
    private Attitude _lastAttitude;
    private bool _hasPosition;
    private bool _hasAttitude;

    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private bool _disposed;

    /// <summary>
    /// Initialises a new <see cref="ArduPilotSitlBackend"/> using <see cref="IOptions{T}"/> for DI.
    /// </summary>
    /// <param name="options">Backend options wrapped in IOptions.</param>
    /// <param name="processManager">SITL process lifecycle manager.</param>
    public ArduPilotSitlBackend(
        IOptions<SitlBackendOptions> options,
        SitlProcessManager processManager)
        : this(options.Value, processManager)
    {
    }

    /// <summary>
    /// Initialises a new <see cref="ArduPilotSitlBackend"/> with raw options.
    /// </summary>
    /// <param name="options">Backend options.</param>
    /// <param name="processManager">SITL process lifecycle manager.</param>
    public ArduPilotSitlBackend(SitlBackendOptions options, SitlProcessManager processManager)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
    }

    /// <inheritdoc/>
    public FlightBackendCapabilities Capabilities =>
        FlightBackendCapabilities.Gps | FlightBackendCapabilities.WindInjection;

    /// <inheritdoc/>
    /// <remarks>
    /// Spawns the ArduPilot SITL process for the configured instance index, then
    /// establishes a <see cref="MavlinkConnection"/> over UDP and starts the background
    /// telemetry receive loop.
    /// </remarks>
    public async ValueTask InitializeAsync(DroneConfig config, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _processManager.SpawnAsync(_options.InstanceIndex, ct).ConfigureAwait(false);

        var mavPort = _processManager.GetMavlinkPort(_options.InstanceIndex);

        // Local GCS listens on a port offset from the vehicle port.
        var localListenPort = mavPort + 1;

        _transport = new UdpTransport(new UdpTransportOptions
        {
            ListenPort = localListenPort,
            RemotePort = mavPort,
            RemoteHost = "127.0.0.1",
        });

        _connection = new MavlinkConnection(_transport, new MavlinkConnectionOptions
        {
            HeartbeatInterval = TimeSpan.FromSeconds(1),
        });

        _receiveCts = new CancellationTokenSource();
        _receiveTask = RunReceiveLoopAsync(_receiveCts.Token);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Reads the latest telemetry snapshot captured by the background receive loop and
    /// maps it to a <see cref="DronePhysicsState"/>. If no telemetry has been received yet,
    /// returns a zeroed-out state at the drone's start position.
    /// </remarks>
    public ValueTask<DronePhysicsState> StepAsync(double dt, Vector3 wind, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        GlobalPositionInt position;
        Attitude attitude;
        bool hasData;

        lock (_telemetryLock)
        {
            position = _lastPosition;
            attitude = _lastAttitude;
            hasData = _hasPosition && _hasAttitude;
        }

        if (!hasData)
            return ValueTask.FromResult(DronePhysicsState.AtPosition(Vector3.Zero));

        var state = SitlTelemetryMapper.Map(position, attitude);
        return ValueTask.FromResult(state);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Translates the <see cref="FlightCommand"/> to the appropriate MAVLink message
    /// (<see cref="CommandLong"/> for mode-level commands, or <see cref="SetPositionTargetGlobalInt"/>
    /// for waypoint navigation) and sends it via the active connection.
    /// </remarks>
    public async ValueTask SendCommandAsync(FlightCommand command, CancellationToken ct = default)
    {
        if (_connection is null)
            throw new InvalidOperationException("Backend is not initialized. Call InitializeAsync first.");

        IMavlinkMessage message = command.Type switch
        {
            FlightCommandType.Hover => new CommandLong
            {
                Command = MavCmd.NavLoiterUnlim,
                TargetSystem = 1,
                TargetComponent = 1,
            },
            FlightCommandType.Land => new CommandLong
            {
                Command = MavCmd.NavLand,
                TargetSystem = 1,
                TargetComponent = 1,
            },
            FlightCommandType.ReturnToLaunch => new CommandLong
            {
                Command = MavCmd.NavReturnToLaunch,
                TargetSystem = 1,
                TargetComponent = 1,
            },
            FlightCommandType.GoToWaypoint when command.TargetPosition.HasValue => BuildWaypointMessage(command),
            _ => throw new ArgumentException($"Unsupported flight command type: {command.Type}", nameof(command)),
        };

        await _connection.SendMessageAsync(message, ct).ConfigureAwait(false);
    }

    private static SetPositionTargetGlobalInt BuildWaypointMessage(FlightCommand command)
    {
        // Approximate inverse of equirectangular: convert metres back to degE7.
        // This is a simplification — a production implementation would use the home location.
        var target = command.TargetPosition!.Value;
        const double MetresPerDeg = 111_319.49;
        int latDegE7 = (int)(-target.Z / MetresPerDeg * 1e7);
        int lonDegE7 = (int)(target.X / MetresPerDeg * 1e7);
        int altMm = (int)(target.Y * 1000.0);

        return new SetPositionTargetGlobalInt
        {
            TargetSystem = 1,
            TargetComponent = 1,
            CoordinateFrame = MavFrame.GlobalRelativeAlt,
            TypeMask = 0b1111_1111_1000, // position only; ignore vel/accel/yaw
            LatInt = latDegE7,
            LonInt = lonDegE7,
            Alt = target.Y,
        };
    }

    private async Task RunReceiveLoopAsync(CancellationToken ct)
    {
        if (_transport is null)
            return;

        await foreach (var packet in _transport.ReceiveAsync(ct).ConfigureAwait(false))
        {
            if (packet.MessageId == 33) // GLOBAL_POSITION_INT
            {
                var msg = GlobalPositionInt.Deserialize(packet.Payload.Span);
                lock (_telemetryLock)
                {
                    _lastPosition = msg;
                    _hasPosition = true;
                }
            }
            else if (packet.MessageId == 30) // ATTITUDE
            {
                var msg = Attitude.Deserialize(packet.Payload.Span);
                lock (_telemetryLock)
                {
                    _lastAttitude = msg;
                    _hasAttitude = true;
                }
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_receiveCts is not null)
        {
            await _receiveCts.CancelAsync().ConfigureAwait(false);

            if (_receiveTask is not null)
            {
                try
                {
                    await _receiveTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected on shutdown.
                }
            }

            _receiveCts.Dispose();
        }

        if (_connection is not null)
            await _connection.DisposeAsync().ConfigureAwait(false);

        if (_transport is not null)
            await _transport.DisposeAsync().ConfigureAwait(false);

        await _processManager.KillAsync(_options.InstanceIndex).ConfigureAwait(false);
    }
}
