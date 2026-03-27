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
/// MAVLink TERRAIN_REPORT message (ID 136). Response from a TERRAIN_CHECK request.
/// </summary>
public readonly record struct TerrainReport : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 22;

    /// <summary>Latitude (degrees * 1e7).</summary>
    public int Lat { get; init; }

    /// <summary>Longitude (degrees * 1e7).</summary>
    public int Lon { get; init; }

    /// <summary>Terrain height MSL in meters.</summary>
    public float TerrainHeight { get; init; }

    /// <summary>Current vehicle height above lat/lon terrain height in meters.</summary>
    public float CurrentHeight { get; init; }

    /// <summary>Grid spacing.</summary>
    public ushort Spacing { get; init; }

    /// <summary>Number of 4x4 terrain blocks waiting to be received or read from disk.</summary>
    public ushort Pending { get; init; }

    /// <summary>Number of 4x4 terrain blocks in memory.</summary>
    public ushort Loaded { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 136;

    /// <inheritdoc/>
    public byte CrcExtra => 1;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, Lat);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], Lon);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], TerrainHeight);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], CurrentHeight);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[16..], Spacing);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[18..], Pending);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[20..], Loaded);
    }

    /// <summary>Deserializes a <see cref="TerrainReport"/> from a raw payload span.</summary>
    public static TerrainReport Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            Lat = BinaryPrimitives.ReadInt32LittleEndian(buffer),
            Lon = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
            TerrainHeight = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            CurrentHeight = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Spacing = BinaryPrimitives.ReadUInt16LittleEndian(buffer[16..]),
            Pending = BinaryPrimitives.ReadUInt16LittleEndian(buffer[18..]),
            Loaded = BinaryPrimitives.ReadUInt16LittleEndian(buffer[20..]),
        };
}
