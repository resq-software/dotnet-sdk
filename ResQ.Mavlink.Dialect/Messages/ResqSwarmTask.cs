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
/// RESQ_SWARM_TASK (ID 60002). Assigns a mission task to a specific drone in the swarm.
/// CRC extra: 211 — derived from RESQ_SWARM_TASK field layout hash.
/// Layout (34 bytes): TaskId(4) AreaLat1E7(4) AreaLon1E7(4) AreaLat2E7(4) AreaLon2E7(4) AltMinMm(4) AltMaxMm(4)
///   TimeoutSec(2) TargetDroneId(1) TaskType(1) Priority(1) SearchPattern(1).
/// </summary>
public readonly record struct ResqSwarmTask : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 34;

    /// <inheritdoc/>
    public uint MessageId => 60002;

    /// <inheritdoc/>
    public byte CrcExtra => 211;

    /// <summary>Unique task identifier.</summary>
    public uint TaskId { get; init; }

    /// <summary>Area corner 1 latitude in degE7.</summary>
    public int AreaLat1E7 { get; init; }

    /// <summary>Area corner 1 longitude in degE7.</summary>
    public int AreaLon1E7 { get; init; }

    /// <summary>Area corner 2 latitude in degE7.</summary>
    public int AreaLat2E7 { get; init; }

    /// <summary>Area corner 2 longitude in degE7.</summary>
    public int AreaLon2E7 { get; init; }

    /// <summary>Minimum altitude in millimetres.</summary>
    public int AltMinMm { get; init; }

    /// <summary>Maximum altitude in millimetres.</summary>
    public int AltMaxMm { get; init; }

    /// <summary>Task timeout in seconds.</summary>
    public ushort TimeoutSec { get; init; }

    /// <summary>System ID of the assigned drone.</summary>
    public byte TargetDroneId { get; init; }

    /// <summary>Type of task to perform.</summary>
    public ResqTaskType TaskType { get; init; }

    /// <summary>Task priority.</summary>
    public ResqTaskPriority Priority { get; init; }

    /// <summary>Search pattern to use for the task.</summary>
    public ResqSearchPattern SearchPattern { get; init; }

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TaskId);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], AreaLat1E7);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], AreaLon1E7);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], AreaLat2E7);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], AreaLon2E7);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[20..], AltMinMm);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[24..], AltMaxMm);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[28..], TimeoutSec);
        buffer[30] = TargetDroneId;
        buffer[31] = (byte)TaskType;
        buffer[32] = (byte)Priority;
        buffer[33] = (byte)SearchPattern;
    }

    /// <summary>Deserializes a <see cref="ResqSwarmTask"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="ResqSwarmTask"/>.</returns>
    public static ResqSwarmTask Deserialize(ReadOnlySpan<byte> buffer) => new()
    {
        TaskId = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
        AreaLat1E7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
        AreaLon1E7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
        AreaLat2E7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
        AreaLon2E7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]),
        AltMinMm = BinaryPrimitives.ReadInt32LittleEndian(buffer[20..]),
        AltMaxMm = BinaryPrimitives.ReadInt32LittleEndian(buffer[24..]),
        TimeoutSec = BinaryPrimitives.ReadUInt16LittleEndian(buffer[28..]),
        TargetDroneId = buffer[30],
        TaskType = (ResqTaskType)buffer[31],
        Priority = (ResqTaskPriority)buffer[32],
        SearchPattern = (ResqSearchPattern)buffer[33],
    };
}
