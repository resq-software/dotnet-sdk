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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Connection;
using ResQ.Mavlink.Dialect.Messages;
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;

namespace ResQ.Mavlink.Mesh.Firmware;

/// <summary>
/// Bridge API between ArduPilot firmware (or a companion process) and the .NET mesh layer.
/// Allows firmware code to send and receive ResQ dialect messages via the mesh transport.
/// </summary>
public interface IFirmwareIntegration
{
    /// <summary>Called by firmware when a detection is made by onboard AI.</summary>
    ValueTask ReportDetectionAsync(ResqDetection detection, CancellationToken ct = default);

    /// <summary>Called by firmware to get the current swarm task assignment for a drone.</summary>
    ValueTask<ResqSwarmTask?> GetCurrentTaskAsync(byte systemId, CancellationToken ct = default);

    /// <summary>Called by firmware to report task progress.</summary>
    ValueTask ReportTaskProgressAsync(ResqSwarmTaskAck ack, CancellationToken ct = default);

    /// <summary>Called by firmware to broadcast an emergency beacon.</summary>
    ValueTask BroadcastEmergencyAsync(ResqEmergencyBeacon beacon, CancellationToken ct = default);

    /// <summary>Event raised when a new swarm task is assigned to this drone.</summary>
    IAsyncEnumerable<ResqSwarmTask> TaskAssignments(CancellationToken ct = default);

    /// <summary>Event raised when a hazard zone update is received from the network.</summary>
    IAsyncEnumerable<ResqHazardZone> HazardZoneUpdates(CancellationToken ct = default);
}

/// <summary>
/// Configuration for <see cref="FirmwareIntegrationService"/>.
/// </summary>
public sealed class FirmwareIntegrationOptions
{
    /// <summary>The MAVLink system ID of this drone.</summary>
    public byte OwnSystemId { get; set; } = 1;

    /// <summary>The MAVLink component ID of this drone.</summary>
    public byte OwnComponentId { get; set; } = 1;
}

/// <summary>
/// <see cref="IFirmwareIntegration"/> implementation backed by a <see cref="MeshTransport"/>
/// and an in-process MAVLink message dispatch loop.
/// </summary>
public sealed class FirmwareIntegrationService : IFirmwareIntegration, IAsyncDisposable
{
    private readonly IMavlinkTransport _transport;
    private readonly FirmwareIntegrationOptions _options;
    private readonly Channel<ResqSwarmTask> _taskChannel;
    private readonly Channel<ResqHazardZone> _hazardChannel;
    private readonly Dictionary<byte, ResqSwarmTask> _currentTasks = new();
    private readonly object _taskLock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _dispatchTask;
    private byte _sequenceNumber;

    /// <summary>
    /// Initialises a new <see cref="FirmwareIntegrationService"/>.
    /// </summary>
    /// <param name="transport">
    /// The mesh transport (or any <see cref="IMavlinkTransport"/>) to use for sending and receiving.
    /// </param>
    /// <param name="options">Service configuration including the drone's own system ID.</param>
    public FirmwareIntegrationService(
        IMavlinkTransport transport,
        IOptions<FirmwareIntegrationOptions> options)
    {
        _transport = transport;
        _options = options.Value;
        _taskChannel = Channel.CreateUnbounded<ResqSwarmTask>();
        _hazardChannel = Channel.CreateUnbounded<ResqHazardZone>();
        _dispatchTask = RunDispatchAsync(_cts.Token);
    }

    // ── IFirmwareIntegration ─────────────────────────────────────────────

    /// <inheritdoc/>
    public ValueTask ReportDetectionAsync(ResqDetection detection, CancellationToken ct = default)
    {
        var payload = new byte[ResqDetection.PayloadSize];
        detection.Serialize(payload);
        var pkt = BuildPacket(60000, payload);
        return _transport.SendAsync(pkt, ct);
    }

    /// <inheritdoc/>
    public ValueTask<ResqSwarmTask?> GetCurrentTaskAsync(byte systemId, CancellationToken ct = default)
    {
        lock (_taskLock)
        {
            return _currentTasks.TryGetValue(systemId, out var task)
                ? new ValueTask<ResqSwarmTask?>(task)
                : new ValueTask<ResqSwarmTask?>(default(ResqSwarmTask?));
        }
    }

    /// <inheritdoc/>
    public ValueTask ReportTaskProgressAsync(ResqSwarmTaskAck ack, CancellationToken ct = default)
    {
        var payload = new byte[ResqSwarmTaskAck.PayloadSize];
        ack.Serialize(payload);
        var pkt = BuildPacket(60003, payload);
        return _transport.SendAsync(pkt, ct);
    }

    /// <inheritdoc/>
    public ValueTask BroadcastEmergencyAsync(ResqEmergencyBeacon beacon, CancellationToken ct = default)
    {
        var payload = new byte[ResqEmergencyBeacon.PayloadSize];
        beacon.Serialize(payload);
        var pkt = BuildPacket(60007, payload);
        return _transport.SendAsync(pkt, ct);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ResqSwarmTask> TaskAssignments(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var task in _taskChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            yield return task;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ResqHazardZone> HazardZoneUpdates(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var zone in _hazardChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            yield return zone;
    }

    // ── dispatch loop ────────────────────────────────────────────────────

    private async Task RunDispatchAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var pkt in _transport.ReceiveAsync(ct).ConfigureAwait(false))
            {
                switch (pkt.MessageId)
                {
                    case 60002: // RESQ_SWARM_TASK
                        if (pkt.Payload.Length >= ResqSwarmTask.PayloadSize)
                        {
                            var task = ResqSwarmTask.Deserialize(pkt.Payload.Span);
                            lock (_taskLock) _currentTasks[task.TargetDroneId] = task;
                            _taskChannel.Writer.TryWrite(task);
                        }
                        break;

                    case 60004: // RESQ_HAZARD_ZONE
                        if (pkt.Payload.Length >= ResqHazardZone.PayloadSize)
                        {
                            var zone = ResqHazardZone.Deserialize(pkt.Payload.Span);
                            _hazardChannel.Writer.TryWrite(zone);
                        }
                        break;
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _taskChannel.Writer.TryComplete();
            _hazardChannel.Writer.TryComplete();
        }
    }

    private MavlinkPacket BuildPacket(uint messageId, byte[] payload) =>
        new(
            sequenceNumber: _sequenceNumber++,
            systemId: _options.OwnSystemId,
            componentId: _options.OwnComponentId,
            messageId: messageId,
            payload: payload,
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        try { await _dispatchTask.ConfigureAwait(false); } catch { /* ignored */ }
        _cts.Dispose();
    }
}
