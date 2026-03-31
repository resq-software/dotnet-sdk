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

namespace ResQ.Mavlink.Mesh;

/// <summary>
/// Configuration options for <see cref="MeshTransport"/>.
/// </summary>
public sealed class MeshTransportOptions
{
    /// <summary>
    /// Default hop count (TTL) applied to outgoing packets.
    /// </summary>
    public int DefaultTtl { get; set; } = 3;

    /// <summary>
    /// TTL applied to RESQ_EMERGENCY_BEACON messages (ID 60007).
    /// </summary>
    public int EmergencyTtl { get; set; } = 7;

    /// <summary>
    /// Number of entries in the deduplication ring buffer.
    /// </summary>
    public int DeduplicationWindowSize { get; set; } = 256;

    /// <summary>
    /// Maximum number of packets in the priority transmit queue.
    /// When the queue is full the lowest-priority packet is evicted.
    /// </summary>
    public int MaxTransmitQueueSize { get; set; } = 1000;
}
