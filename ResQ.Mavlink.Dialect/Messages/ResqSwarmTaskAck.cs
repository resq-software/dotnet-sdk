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

using System.Buffers.Binary;
using ResQ.Mavlink.Dialect.Enums;
using ResQ.Mavlink.Messages;

namespace ResQ.Mavlink.Dialect.Messages;

/// <summary>
/// RESQ_SWARM_TASK_ACK (ID 60003). Response to a <see cref="ResqSwarmTask"/> from the assigned drone.
/// CRC extra: 55 — derived from RESQ_SWARM_TASK_ACK field layout hash.
/// Layout (6 bytes): TaskId(4) Response(1) ProgressPercent(1).
/// </summary>
public readonly record struct ResqSwarmTaskAck : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 6;

    /// <inheritdoc/>
    public uint MessageId => 60003;

    /// <inheritdoc/>
    public byte CrcExtra => 55;

    /// <summary>Task ID being acknowledged.</summary>
    public uint TaskId { get; init; }

    /// <summary>Response code indicating the task execution result.</summary>
    public ResqTaskResponse Response { get; init; }

    /// <summary>Task progress in percent (0–100).</summary>
    public byte ProgressPercent { get; init; }

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TaskId);
        buffer[4] = (byte)Response;
        buffer[5] = ProgressPercent;
    }

    /// <summary>Deserializes a <see cref="ResqSwarmTaskAck"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="ResqSwarmTaskAck"/>.</returns>
    public static ResqSwarmTaskAck Deserialize(ReadOnlySpan<byte> buffer) => new()
    {
        TaskId = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
        Response = (ResqTaskResponse)buffer[4],
        ProgressPercent = buffer[5],
    };
}
