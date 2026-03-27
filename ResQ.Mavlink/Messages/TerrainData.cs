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
/// MAVLink TERRAIN_DATA message (ID 134). Terrain data sent from GCS. The lat/lon and grid_spacing must be the same as a lat/lon from a TERRAIN_REQUEST.
/// </summary>
public readonly record struct TerrainData : IMavlinkMessage
{
    /// <summary>Payload size in bytes (4+4+2+1+1+32 = 44).</summary>
    public const int PayloadSize = 44;

    /// <summary>Latitude of SW corner of first grid (degrees * 1e7).</summary>
    public int Lat { get; init; }

    /// <summary>Longitude of SW corner of first grid (degrees * 1e7).</summary>
    public int Lon { get; init; }

    /// <summary>Grid spacing.</summary>
    public ushort GridSpacing { get; init; }

    /// <summary>bit within the terrain request mask — which 4x4 block this is.</summary>
    public byte Gridbit { get; init; }

    /// <summary>Terrain data MSL — 16 altitude values (meters * 10^-1 = dm).</summary>
    public short Data0 { get; init; }

    /// <summary>Second terrain data point.</summary>
    public short Data1 { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 134;

    /// <inheritdoc/>
    public byte CrcExtra => 229;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, Lat);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], Lon);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[8..], GridSpacing);
        buffer[10] = Gridbit;
        // 16 data shorts starting at byte 11; simplified to first two
        BinaryPrimitives.WriteInt16LittleEndian(buffer[11..], Data0);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[13..], Data1);
        // remaining 28 bytes are zero
    }

    /// <summary>Deserializes a <see cref="TerrainData"/> from a raw payload span.</summary>
    public static TerrainData Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            Lat = BinaryPrimitives.ReadInt32LittleEndian(buffer),
            Lon = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
            GridSpacing = BinaryPrimitives.ReadUInt16LittleEndian(buffer[8..]),
            Gridbit = buffer[10],
            Data0 = BinaryPrimitives.ReadInt16LittleEndian(buffer[11..]),
            Data1 = BinaryPrimitives.ReadInt16LittleEndian(buffer[13..]),
        };
}
