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
/// MAVLink GLOBAL_POSITION_INT message (ID 33). Filtered GPS position.
/// Lat/Lon in degE7, Alt in mm, velocities in cm/s, heading in cdeg.
/// </summary>
public readonly record struct GlobalPositionInt : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 28;

    /// <summary>Timestamp (time since system boot).</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>Latitude, expressed in degrees * 1E7.</summary>
    public int Lat { get; init; }

    /// <summary>Longitude, expressed in degrees * 1E7.</summary>
    public int Lon { get; init; }

    /// <summary>Altitude (MSL), in millimeters (positive up).</summary>
    public int Alt { get; init; }

    /// <summary>Altitude above ground, in millimeters (positive up).</summary>
    public int RelativeAlt { get; init; }

    /// <summary>Ground X Speed (Latitude, positive north), expressed as cm/s.</summary>
    public short Vx { get; init; }

    /// <summary>Ground Y Speed (Longitude, positive east), expressed as cm/s.</summary>
    public short Vy { get; init; }

    /// <summary>Ground Z Speed (Altitude, positive down), expressed as cm/s.</summary>
    public short Vz { get; init; }

    /// <summary>Vehicle heading (yaw angle), 0.0..359.99 degrees. If unknown, set to: UINT16_MAX.</summary>
    public ushort Hdg { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 33;

    /// <inheritdoc/>
    public byte CrcExtra => 104;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], Lat);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], Lon);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], Alt);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], RelativeAlt);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[20..], Vx);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[22..], Vy);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[24..], Vz);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[26..], Hdg);
    }

    /// <summary>Deserializes a <see cref="GlobalPositionInt"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="GlobalPositionInt"/>.</returns>
    public static GlobalPositionInt Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new GlobalPositionInt
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Lat = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
            Lon = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
            Alt = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
            RelativeAlt = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]),
            Vx = BinaryPrimitives.ReadInt16LittleEndian(buffer[20..]),
            Vy = BinaryPrimitives.ReadInt16LittleEndian(buffer[22..]),
            Vz = BinaryPrimitives.ReadInt16LittleEndian(buffer[24..]),
            Hdg = BinaryPrimitives.ReadUInt16LittleEndian(buffer[26..]),
        };
    }
}
