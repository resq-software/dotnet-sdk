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

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink MISSION_COUNT message (ID 44). This message is emitted as response to MISSION_REQUEST_LIST by the MAV and to initiate a write transaction.
/// </summary>
public readonly record struct MissionCount : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 4;

    /// <summary>Number of mission items in the sequence.</summary>
    public ushort Count { get; init; }

    /// <summary>System ID.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>Component ID.</summary>
    public byte TargetComponent { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 44;

    /// <inheritdoc/>
    public byte CrcExtra => 221;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Count);
        buffer[2] = TargetSystem;
        buffer[3] = TargetComponent;
    }

    /// <summary>Deserializes a <see cref="MissionCount"/> from a raw payload span.</summary>
    public static MissionCount Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new MissionCount
        {
            Count = BinaryPrimitives.ReadUInt16LittleEndian(buffer),
            TargetSystem = buffer[2],
            TargetComponent = buffer[3],
        };
    }
}
