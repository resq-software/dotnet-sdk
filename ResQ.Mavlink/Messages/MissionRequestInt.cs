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
/// MAVLink MISSION_REQUEST_INT message (ID 51). Request the information of the mission item with the sequence number seq.
/// The response to this message is the MISSION_ITEM_INT message.
/// </summary>
public readonly record struct MissionRequestInt : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 4;

    /// <summary>Sequence.</summary>
    public ushort Seq { get; init; }

    /// <summary>System ID.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>Component ID.</summary>
    public byte TargetComponent { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 51;

    /// <inheritdoc/>
    public byte CrcExtra => 196;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Seq);
        buffer[2] = TargetSystem;
        buffer[3] = TargetComponent;
    }

    /// <summary>Deserializes a <see cref="MissionRequestInt"/> from a raw payload span.</summary>
    public static MissionRequestInt Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new MissionRequestInt
        {
            Seq = BinaryPrimitives.ReadUInt16LittleEndian(buffer),
            TargetSystem = buffer[2],
            TargetComponent = buffer[3],
        };
    }
}
