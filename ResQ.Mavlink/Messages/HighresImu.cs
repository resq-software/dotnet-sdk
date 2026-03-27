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
/// MAVLink HIGHRES_IMU message (ID 105). The IMU readings in SI units in NED body frame (high resolution).
/// </summary>
public readonly record struct HighresImu : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 62;

    /// <summary>Timestamp in microseconds.</summary>
    public ulong TimeUsec { get; init; }

    /// <summary>X acceleration in m/s^2.</summary>
    public float Xacc { get; init; }

    /// <summary>Y acceleration in m/s^2.</summary>
    public float Yacc { get; init; }

    /// <summary>Z acceleration in m/s^2.</summary>
    public float Zacc { get; init; }

    /// <summary>Angular speed around X axis in rad/s.</summary>
    public float Xgyro { get; init; }

    /// <summary>Angular speed around Y axis in rad/s.</summary>
    public float Ygyro { get; init; }

    /// <summary>Angular speed around Z axis in rad/s.</summary>
    public float Zgyro { get; init; }

    /// <summary>X Magnetic field in gauss.</summary>
    public float Xmag { get; init; }

    /// <summary>Y Magnetic field in gauss.</summary>
    public float Ymag { get; init; }

    /// <summary>Z Magnetic field in gauss.</summary>
    public float Zmag { get; init; }

    /// <summary>Absolute pressure in hPa.</summary>
    public float AbsPressure { get; init; }

    /// <summary>Differential pressure in hPa.</summary>
    public float DiffPressure { get; init; }

    /// <summary>Altitude calculated from pressure.</summary>
    public float PressureAlt { get; init; }

    /// <summary>Temperature in degrees celsius.</summary>
    public float Temperature { get; init; }

    /// <summary>Bitmap for fields that have updated since last message.</summary>
    public ushort FieldsUpdated { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 105;

    /// <inheritdoc/>
    public byte CrcExtra => 93;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUsec);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Xacc);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Yacc);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Zacc);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], Xgyro);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Ygyro);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], Zgyro);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[32..], Xmag);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[36..], Ymag);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[40..], Zmag);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[44..], AbsPressure);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[48..], DiffPressure);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[52..], PressureAlt);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[56..], Temperature);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[60..], FieldsUpdated);
    }

    /// <summary>Deserializes a <see cref="HighresImu"/> from a raw payload span.</summary>
    public static HighresImu Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUsec = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            Xacc = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Yacc = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Zacc = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Xgyro = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            Ygyro = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            Zgyro = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
            Xmag = BinaryPrimitives.ReadSingleLittleEndian(buffer[32..]),
            Ymag = BinaryPrimitives.ReadSingleLittleEndian(buffer[36..]),
            Zmag = BinaryPrimitives.ReadSingleLittleEndian(buffer[40..]),
            AbsPressure = BinaryPrimitives.ReadSingleLittleEndian(buffer[44..]),
            DiffPressure = BinaryPrimitives.ReadSingleLittleEndian(buffer[48..]),
            PressureAlt = BinaryPrimitives.ReadSingleLittleEndian(buffer[52..]),
            Temperature = BinaryPrimitives.ReadSingleLittleEndian(buffer[56..]),
            FieldsUpdated = BinaryPrimitives.ReadUInt16LittleEndian(buffer[60..]),
        };
}
