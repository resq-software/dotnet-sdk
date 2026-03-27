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
/// MAVLink MISSION_CURRENT message (ID 42). Message that announces the sequence number of the current active mission item.
/// </summary>
public readonly record struct MissionCurrent : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 2;

    /// <summary>Sequence.</summary>
    public ushort Seq { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 42;

    /// <inheritdoc/>
    public byte CrcExtra => 28;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Seq);
    }

    /// <summary>Deserializes a <see cref="MissionCurrent"/> from a raw payload span.</summary>
    public static MissionCurrent Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new MissionCurrent
        {
            Seq = BinaryPrimitives.ReadUInt16LittleEndian(buffer),
        };
    }
}
