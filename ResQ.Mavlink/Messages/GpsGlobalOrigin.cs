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
/// MAVLink GPS_GLOBAL_ORIGIN message (ID 49). Once the system is booted and ready, this message indicates the global GPS origin.
/// </summary>
public readonly record struct GpsGlobalOrigin : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 12;

    /// <summary>Latitude (WGS84) in degrees * 1e7.</summary>
    public int Latitude { get; init; }

    /// <summary>Longitude (WGS84) in degrees * 1e7.</summary>
    public int Longitude { get; init; }

    /// <summary>Altitude (MSL) in mm.</summary>
    public int Altitude { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 49;

    /// <inheritdoc/>
    public byte CrcExtra => 39;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, Latitude);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], Longitude);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], Altitude);
    }

    /// <summary>Deserializes a <see cref="GpsGlobalOrigin"/> from a raw payload span.</summary>
    public static GpsGlobalOrigin Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            Latitude = BinaryPrimitives.ReadInt32LittleEndian(buffer),
            Longitude = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
            Altitude = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
        };
}
