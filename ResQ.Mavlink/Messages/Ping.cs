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
/// MAVLink PING message (ID 4). A ping message either requesting or responding to a ping.
/// </summary>
public readonly record struct Ping : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 14;

    /// <summary>Timestamp in microseconds.</summary>
    public ulong TimeUsec { get; init; }

    /// <summary>PING sequence.</summary>
    public uint Seq { get; init; }

    /// <summary>0: request ping from all receiving systems, or specific system ID.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>0: request ping from all receiving components, or specific component ID.</summary>
    public byte TargetComponent { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 4;

    /// <inheritdoc/>
    public byte CrcExtra => 237;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUsec);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], Seq);
        buffer[12] = TargetSystem;
        buffer[13] = TargetComponent;
    }

    /// <summary>Deserializes a <see cref="Ping"/> from a raw payload span.</summary>
    public static Ping Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUsec = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            Seq = BinaryPrimitives.ReadUInt32LittleEndian(buffer[8..]),
            TargetSystem = buffer[12],
            TargetComponent = buffer[13],
        };
}
