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
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;

namespace ResQ.Mavlink.Mesh;

/// <summary>
/// A mesh-aware <see cref="IMavlinkTransport"/> that wraps an underlying radio transport and adds
/// TTL-based flooding, packet deduplication, and priority-based transmit queuing.
/// </summary>
/// <remarks>
/// <para>Message priority (lower number = higher priority):</para>
/// <list type="bullet">
///   <item>Emergency beacon (60007): 0</item>
///   <item>Detection (60000): 1</item>
///   <item>Commands (60001–60003): 2</item>
///   <item>All other messages: 10</item>
/// </list>
/// </remarks>
public sealed class MeshTransport : IMavlinkTransport
{
    private const uint EmergencyBeaconId = 60007;
    private const uint DetectionId = 60000;

    private readonly IMavlinkTransport _inner;
    private readonly MeshTransportOptions _options;

    // Priority transmit queue — lower priority value = dequeued first.
    private readonly PriorityQueue<MavlinkPacket, int> _txQueue;
    private int _txWorstPriority; // highest numeric priority (= lowest urgency) currently in the queue
    private readonly SemaphoreSlim _txSignal = new(0);
    private readonly object _txLock = new();

    // Deduplication ring buffer keyed by (systemId << 8 | sequenceNumber).
    private readonly int[] _dedupRing;
    private int _dedupHead;
    private readonly HashSet<int> _dedupSet;
    private readonly object _dedupLock = new();

    private readonly Channel<MavlinkPacket> _rxChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pumpTask;

    /// <inheritdoc/>
    public TransportState State => _inner.State;

    /// <summary>
    /// Initialises a new <see cref="MeshTransport"/> wrapping <paramref name="inner"/>.
    /// </summary>
    /// <param name="inner">The underlying radio transport (UDP for sim, serial for real hardware).</param>
    /// <param name="options">Mesh transport configuration.</param>
    public MeshTransport(IMavlinkTransport inner, IOptions<MeshTransportOptions> options)
    {
        _inner = inner;
        _options = options.Value;
        _txQueue = new PriorityQueue<MavlinkPacket, int>();
        _txWorstPriority = int.MinValue;
        _dedupRing = new int[_options.DeduplicationWindowSize];
        _dedupSet = new HashSet<int>(_options.DeduplicationWindowSize);
        for (var i = 0; i < _dedupRing.Length; i++) _dedupRing[i] = -1;
        _rxChannel = Channel.CreateUnbounded<MavlinkPacket>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });
        _pumpTask = Task.WhenAll(
            RunTxPumpAsync(_cts.Token),
            RunRxPumpAsync(_cts.Token));
    }

    /// <summary>
    /// Returns the message priority for the transmit queue (lower = higher priority).
    /// </summary>
    public static int GetPriority(uint messageId) => messageId switch
    {
        60007 => 0, // RESQ_EMERGENCY_BEACON
        60000 => 1, // RESQ_DETECTION
        60001 or 60002 or 60003 => 2, // swarm / task messages
        _ => 10,
    };

    /// <inheritdoc/>
    public ValueTask SendAsync(MavlinkPacket packet, CancellationToken ct = default)
    {
        var ttl = packet.MessageId == EmergencyBeaconId
            ? _options.EmergencyTtl
            : _options.DefaultTtl;

        // Encode TTL in the compatFlags byte (bits 0-3).
        var flaggedPacket = new MavlinkPacket(
            packet.SequenceNumber,
            packet.SystemId,
            packet.ComponentId,
            packet.MessageId,
            packet.Payload,
            packet.IncompatFlags,
            (byte)((packet.CompatFlags & 0xF0) | (ttl & 0x0F)),
            packet.Signature);

        var priority = GetPriority(packet.MessageId);

        lock (_txLock)
        {
            if (_txQueue.Count >= _options.MaxTransmitQueueSize)
            {
                // Drop incoming if it is not more urgent (lower number) than the worst in queue.
                if (priority >= _txWorstPriority)
                    return ValueTask.CompletedTask;

                // Incoming is more urgent — drain the queue, discard the single worst item,
                // re-enqueue the rest, then add the new packet.
                var snapshot = new List<(MavlinkPacket pkt, int pri)>(_txQueue.Count);
                while (_txQueue.TryDequeue(out var p, out var pri))
                    snapshot.Add((p, pri));

                // Find index of the item with the highest priority number (lowest urgency).
                int worstIdx = 0;
                for (int i = 1; i < snapshot.Count; i++)
                {
                    if (snapshot[i].pri > snapshot[worstIdx].pri)
                        worstIdx = i;
                }
                snapshot.RemoveAt(worstIdx);

                _txWorstPriority = int.MinValue;
                foreach (var (p, pri) in snapshot)
                {
                    _txQueue.Enqueue(p, pri);
                    if (pri > _txWorstPriority) _txWorstPriority = pri;
                }
                _txQueue.Enqueue(flaggedPacket, priority);
                if (priority > _txWorstPriority) _txWorstPriority = priority;
            }
            else
            {
                _txQueue.Enqueue(flaggedPacket, priority);
                if (priority > _txWorstPriority)
                    _txWorstPriority = priority;
            }
        }
        _txSignal.Release();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<MavlinkPacket> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var pkt in _rxChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            yield return pkt;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<TransportState> StateChanges(CancellationToken ct = default)
        => _inner.StateChanges(ct);

    // ── background tasks ─────────────────────────────────────────────────

    private async Task RunTxPumpAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _txSignal.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            MavlinkPacket? pkt;
            lock (_txLock)
            {
                _txQueue.TryDequeue(out pkt, out _);
                if (_txQueue.Count == 0)
                    _txWorstPriority = int.MinValue;
            }
            if (pkt is null) continue;

            try
            {
                await _inner.SendAsync(pkt, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunRxPumpAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var pkt in _inner.ReceiveAsync(ct).ConfigureAwait(false))
            {
                var key = (pkt.SystemId << 8) | pkt.SequenceNumber;

                bool isDuplicate;
                lock (_dedupLock)
                {
                    isDuplicate = _dedupSet.Contains(key);
                    if (!isDuplicate)
                    {
                        // Evict oldest entry from ring buffer
                        var evictKey = _dedupRing[_dedupHead];
                        if (evictKey >= 0)
                            _dedupSet.Remove(evictKey);
                        _dedupRing[_dedupHead] = key;
                        _dedupSet.Add(key);
                        _dedupHead = (_dedupHead + 1) % _dedupRing.Length;
                    }
                }

                if (isDuplicate) continue;

                // Rebroadcast if TTL > 0
                var ttl = pkt.CompatFlags & 0x0F;
                if (ttl > 0)
                {
                    var relayed = new MavlinkPacket(
                        pkt.SequenceNumber,
                        pkt.SystemId,
                        pkt.ComponentId,
                        pkt.MessageId,
                        pkt.Payload,
                        pkt.IncompatFlags,
                        (byte)((pkt.CompatFlags & 0xF0) | ((ttl - 1) & 0x0F)),
                        pkt.Signature);
                    try
                    {
                        await _inner.SendAsync(relayed, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                // Deliver to local consumers
                await _rxChannel.Writer.WriteAsync(pkt, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _rxChannel.Writer.TryComplete();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        try { await _pumpTask.ConfigureAwait(false); } catch { /* ignored */ }
        _cts.Dispose();
        _txSignal.Dispose();
        await _inner.DisposeAsync().ConfigureAwait(false);
    }
}
