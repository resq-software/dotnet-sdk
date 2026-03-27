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
/// MAVLink TERRAIN_REQUEST message (ID 133). Request for terrain data and target altitude.
/// </summary>
public readonly record struct TerrainRequest : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 18;

    /// <summary>Bitmask of requested 4x4 grids (52 bit block of data).</summary>
    public ulong Mask { get; init; }

    /// <summary>Latitude of SW corner of first grid (degrees * 1e7).</summary>
    public int Lat { get; init; }

    /// <summary>Longitude of SW corner of first grid (degrees * 1e7).</summary>
    public int Lon { get; init; }

    /// <summary>Grid spacing, 0 = not valid.</summary>
    public ushort GridSpacing { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 133;

    /// <inheritdoc/>
    public byte CrcExtra => 6;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, Mask);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], Lat);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], Lon);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[16..], GridSpacing);
    }

    /// <summary>Deserializes a <see cref="TerrainRequest"/> from a raw payload span.</summary>
    public static TerrainRequest Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            Mask = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            Lat = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
            Lon = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
            GridSpacing = BinaryPrimitives.ReadUInt16LittleEndian(buffer[16..]),
        };
}
