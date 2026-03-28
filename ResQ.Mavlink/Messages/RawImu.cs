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
/// MAVLink RAW_IMU message (ID 27). The RAW IMU readings for a 9DOF sensor.
/// </summary>
public readonly record struct RawImu : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 26;

    /// <summary>Timestamp in microseconds.</summary>
    public ulong TimeUsec { get; init; }

    /// <summary>X acceleration (raw).</summary>
    public short Xacc { get; init; }

    /// <summary>Y acceleration (raw).</summary>
    public short Yacc { get; init; }

    /// <summary>Z acceleration (raw).</summary>
    public short Zacc { get; init; }

    /// <summary>Angular speed around X axis (raw).</summary>
    public short Xgyro { get; init; }

    /// <summary>Angular speed around Y axis (raw).</summary>
    public short Ygyro { get; init; }

    /// <summary>Angular speed around Z axis (raw).</summary>
    public short Zgyro { get; init; }

    /// <summary>X Magnetic field (raw).</summary>
    public short Xmag { get; init; }

    /// <summary>Y Magnetic field (raw).</summary>
    public short Ymag { get; init; }

    /// <summary>Z Magnetic field (raw).</summary>
    public short Zmag { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 27;

    /// <inheritdoc/>
    public byte CrcExtra => 144;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUsec);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[8..], Xacc);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[10..], Yacc);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[12..], Zacc);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[14..], Xgyro);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[16..], Ygyro);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[18..], Zgyro);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[20..], Xmag);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[22..], Ymag);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[24..], Zmag);
    }

    /// <summary>Deserializes a <see cref="RawImu"/> from a raw payload span.</summary>
    public static RawImu Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUsec = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            Xacc = BinaryPrimitives.ReadInt16LittleEndian(buffer[8..]),
            Yacc = BinaryPrimitives.ReadInt16LittleEndian(buffer[10..]),
            Zacc = BinaryPrimitives.ReadInt16LittleEndian(buffer[12..]),
            Xgyro = BinaryPrimitives.ReadInt16LittleEndian(buffer[14..]),
            Ygyro = BinaryPrimitives.ReadInt16LittleEndian(buffer[16..]),
            Zgyro = BinaryPrimitives.ReadInt16LittleEndian(buffer[18..]),
            Xmag = BinaryPrimitives.ReadInt16LittleEndian(buffer[20..]),
            Ymag = BinaryPrimitives.ReadInt16LittleEndian(buffer[22..]),
            Zmag = BinaryPrimitives.ReadInt16LittleEndian(buffer[24..]),
        };
}
