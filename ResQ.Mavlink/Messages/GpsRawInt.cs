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
using ResQ.Mavlink.Enums;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink GPS_RAW_INT message (ID 24). The global position, as returned by the Global Positioning System (GPS).
/// </summary>
public readonly record struct GpsRawInt : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 30;

    /// <summary>Timestamp (UNIX Epoch time or time since system boot).</summary>
    public ulong TimeUsec { get; init; }

    /// <summary>Latitude (WGS84, EGM96 ellipsoid), in degrees * 1E7.</summary>
    public int Lat { get; init; }

    /// <summary>Longitude (WGS84, EGM96 ellipsoid), in degrees * 1E7.</summary>
    public int Lon { get; init; }

    /// <summary>Altitude (MSL). Positive for up. Note that virtually all GPS modules provide the MSL altitude in addition to the WGS84 altitude. In millimeters.</summary>
    public int Alt { get; init; }

    /// <summary>GPS HDOP horizontal dilution of position (unitless * 100).</summary>
    public ushort Eph { get; init; }

    /// <summary>GPS VDOP vertical dilution of position (unitless * 100).</summary>
    public ushort Epv { get; init; }

    /// <summary>GPS ground speed. If unknown, set to: UINT16_MAX. In cm/s.</summary>
    public ushort Vel { get; init; }

    /// <summary>Course over ground (NOT heading, but direction of movement) in degrees * 100, 0.0..359.99 degrees.</summary>
    public ushort Cog { get; init; }

    /// <summary>GPS fix type.</summary>
    public GpsFixType FixType { get; init; }

    /// <summary>Number of satellites visible. If unknown, set to 255.</summary>
    public byte SatellitesVisible { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 24;

    /// <inheritdoc/>
    public byte CrcExtra => 24;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUsec);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], Lat);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], Lon);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], Alt);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[20..], Eph);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[22..], Epv);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[24..], Vel);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[26..], Cog);
        buffer[28] = (byte)FixType;
        buffer[29] = SatellitesVisible;
    }

    /// <summary>Deserializes a <see cref="GpsRawInt"/> from a raw payload span.</summary>
    public static GpsRawInt Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new GpsRawInt
        {
            TimeUsec = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            Lat = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
            Lon = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
            Alt = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]),
            Eph = BinaryPrimitives.ReadUInt16LittleEndian(buffer[20..]),
            Epv = BinaryPrimitives.ReadUInt16LittleEndian(buffer[22..]),
            Vel = BinaryPrimitives.ReadUInt16LittleEndian(buffer[24..]),
            Cog = BinaryPrimitives.ReadUInt16LittleEndian(buffer[26..]),
            FixType = (GpsFixType)buffer[28],
            SatellitesVisible = buffer[29],
        };
    }
}
