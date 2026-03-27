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
/// MAVLink SCALED_IMU2 message (ID 116). The IMU readings in SI units in NED body frame from the second IMU.
/// </summary>
public readonly record struct ScaledImu2 : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 22;

    /// <summary>Timestamp (time since system boot) in ms.</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>X acceleration in mG.</summary>
    public short Xacc { get; init; }

    /// <summary>Y acceleration in mG.</summary>
    public short Yacc { get; init; }

    /// <summary>Z acceleration in mG.</summary>
    public short Zacc { get; init; }

    /// <summary>Angular speed around X axis in millirad/s.</summary>
    public short Xgyro { get; init; }

    /// <summary>Angular speed around Y axis in millirad/s.</summary>
    public short Ygyro { get; init; }

    /// <summary>Angular speed around Z axis in millirad/s.</summary>
    public short Zgyro { get; init; }

    /// <summary>X Magnetic field in mgauss.</summary>
    public short Xmag { get; init; }

    /// <summary>Y Magnetic field in mgauss.</summary>
    public short Ymag { get; init; }

    /// <summary>Z Magnetic field in mgauss.</summary>
    public short Zmag { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 116;

    /// <inheritdoc/>
    public byte CrcExtra => 76;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[4..], Xacc);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[6..], Yacc);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[8..], Zacc);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[10..], Xgyro);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[12..], Ygyro);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[14..], Zgyro);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[16..], Xmag);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[18..], Ymag);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[20..], Zmag);
    }

    /// <summary>Deserializes a <see cref="ScaledImu2"/> from a raw payload span.</summary>
    public static ScaledImu2 Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Xacc = BinaryPrimitives.ReadInt16LittleEndian(buffer[4..]),
            Yacc = BinaryPrimitives.ReadInt16LittleEndian(buffer[6..]),
            Zacc = BinaryPrimitives.ReadInt16LittleEndian(buffer[8..]),
            Xgyro = BinaryPrimitives.ReadInt16LittleEndian(buffer[10..]),
            Ygyro = BinaryPrimitives.ReadInt16LittleEndian(buffer[12..]),
            Zgyro = BinaryPrimitives.ReadInt16LittleEndian(buffer[14..]),
            Xmag = BinaryPrimitives.ReadInt16LittleEndian(buffer[16..]),
            Ymag = BinaryPrimitives.ReadInt16LittleEndian(buffer[18..]),
            Zmag = BinaryPrimitives.ReadInt16LittleEndian(buffer[20..]),
        };
}
