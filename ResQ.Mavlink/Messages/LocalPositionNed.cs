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
/// MAVLink LOCAL_POSITION_NED message (ID 32). The filtered local position in the NED frame.
/// </summary>
public readonly record struct LocalPositionNed : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 28;

    /// <summary>Timestamp (time since system boot) in ms.</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>X Position in m.</summary>
    public float X { get; init; }

    /// <summary>Y Position in m.</summary>
    public float Y { get; init; }

    /// <summary>Z Position in m.</summary>
    public float Z { get; init; }

    /// <summary>X Speed in m/s.</summary>
    public float Vx { get; init; }

    /// <summary>Y Speed in m/s.</summary>
    public float Vy { get; init; }

    /// <summary>Z Speed in m/s.</summary>
    public float Vz { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 32;

    /// <inheritdoc/>
    public byte CrcExtra => 185;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], X);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Y);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Z);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Vx);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], Vy);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Vz);
    }

    /// <summary>Deserializes a <see cref="LocalPositionNed"/> from a raw payload span.</summary>
    public static LocalPositionNed Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            X = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            Y = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Z = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Vx = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Vy = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            Vz = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
        };
}
