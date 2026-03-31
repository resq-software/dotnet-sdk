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
/// MAVLink TERRAIN_CHECK message (ID 135). Request that the vehicle report terrain height at the given location.
/// </summary>
public readonly record struct TerrainCheck : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 8;

    /// <summary>Latitude (degrees * 1e7).</summary>
    public int Lat { get; init; }

    /// <summary>Longitude (degrees * 1e7).</summary>
    public int Lon { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 135;

    /// <inheritdoc/>
    public byte CrcExtra => 203;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, Lat);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], Lon);
    }

    /// <summary>Deserializes a <see cref="TerrainCheck"/> from a raw payload span.</summary>
    public static TerrainCheck Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            Lat = BinaryPrimitives.ReadInt32LittleEndian(buffer),
            Lon = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
        };
}
