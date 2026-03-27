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
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;

namespace ResQ.Mavlink.Mesh;

/// <summary>
/// Store-and-forward relay that buffers <see cref="MavlinkPacket"/> instances during
/// mesh partitions and flushes them when a ground link is restored.
/// </summary>
public sealed class MeshRelay
{
    private readonly MeshRelayOptions _options;

    // Sorted list where index 0 is the lowest priority number (highest urgency).
    // We keep them in insertion order per-priority and evict the back (lowest priority, lowest urgency).
    private readonly List<(MavlinkPacket Packet, int Priority)> _buffer = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initialises a new <see cref="MeshRelay"/>.
    /// </summary>
    /// <param name="options">Relay configuration.</param>
    public MeshRelay(IOptions<MeshRelayOptions> options)
        => _options = options.Value;

    /// <summary>Gets the number of packets currently buffered.</summary>
    public int BufferedCount
    {
        get { lock (_lock) return _buffer.Count; }
    }

    /// <summary>Gets whether the relay is actively buffering (buffer is non-empty).</summary>
    public bool IsBuffering => BufferedCount > 0;

    /// <summary>
    /// Buffers a packet for deferred forwarding when the ground link is restored.
    /// </summary>
    /// <param name="packet">The packet to buffer.</param>
    /// <param name="priority">
    /// Transmit priority (lower = higher urgency, e.g. 0 = emergency, 10 = telemetry).
    /// </param>
    public void BufferMessage(MavlinkPacket packet, int priority)
    {
        lock (_lock)
        {
            if (_buffer.Count >= _options.MaxBufferSize)
            {
                if (!_options.PriorityEviction) return; // drop new packet

                // Find lowest-priority (highest number) item to evict.
                var worstIdx = 0;
                var worstPriority = _buffer[0].Priority;
                for (var i = 1; i < _buffer.Count; i++)
                {
                    if (_buffer[i].Priority > worstPriority)
                    {
                        worstPriority = _buffer[i].Priority;
                        worstIdx = i;
                    }
                }

                if (priority >= worstPriority)
                    return; // new packet is no better — drop it

                _buffer.RemoveAt(worstIdx);
            }

            _buffer.Add((packet, priority));
        }
    }

    /// <summary>
    /// Sends all buffered packets via <paramref name="groundTransport"/> in priority order,
    /// then clears the buffer.
    /// </summary>
    /// <param name="groundTransport">The transport connected to the ground station.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task FlushAsync(IMavlinkTransport groundTransport, CancellationToken ct = default)
    {
        List<(MavlinkPacket Packet, int Priority)> toFlush;
        lock (_lock)
        {
            toFlush = new List<(MavlinkPacket, int)>(_buffer);
            _buffer.Clear();
        }

        // Sort by ascending priority (lower number first = higher urgency first).
        toFlush.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var (pkt, _) in toFlush)
        {
            ct.ThrowIfCancellationRequested();
            await groundTransport.SendAsync(pkt, ct).ConfigureAwait(false);
        }
    }
}
