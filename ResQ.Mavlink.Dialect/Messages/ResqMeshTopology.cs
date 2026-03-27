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
using ResQ.Mavlink.Messages;

namespace ResQ.Mavlink.Dialect.Messages;

/// <summary>
/// RESQ_MESH_TOPOLOGY (ID 60005). Reports a drone's mesh network neighbours and link quality.
/// CRC extra: 97 — derived from RESQ_MESH_TOPOLOGY field layout hash.
/// Layout (22 bytes): TimestampMs(8) ReporterSystemId(1) NeighborCount(1)
///   Neighbor1Id(1) Neighbor1Rssi(1) Neighbor2Id(1) Neighbor2Rssi(1)
///   Neighbor3Id(1) Neighbor3Rssi(1) Neighbor4Id(1) Neighbor4Rssi(1)
///   Neighbor5Id(1) Neighbor5Rssi(1) HasGroundLink(1).
/// </summary>
public readonly record struct ResqMeshTopology : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 22;

    /// <inheritdoc/>
    public uint MessageId => 60005;

    /// <inheritdoc/>
    public byte CrcExtra => 97;

    /// <summary>Unix timestamp in milliseconds.</summary>
    public ulong TimestampMs { get; init; }

    /// <summary>System ID of the reporting drone.</summary>
    public byte ReporterSystemId { get; init; }

    /// <summary>Number of active neighbours (0–5).</summary>
    public byte NeighborCount { get; init; }

    /// <summary>Neighbour 1 system ID (0 = unused).</summary>
    public byte Neighbor1Id { get; init; }

    /// <summary>Neighbour 1 RSSI in dBm (unsigned byte, e.g. 200 = -56 dBm via convention).</summary>
    public byte Neighbor1Rssi { get; init; }

    /// <summary>Neighbour 2 system ID (0 = unused).</summary>
    public byte Neighbor2Id { get; init; }

    /// <summary>Neighbour 2 RSSI.</summary>
    public byte Neighbor2Rssi { get; init; }

    /// <summary>Neighbour 3 system ID (0 = unused).</summary>
    public byte Neighbor3Id { get; init; }

    /// <summary>Neighbour 3 RSSI.</summary>
    public byte Neighbor3Rssi { get; init; }

    /// <summary>Neighbour 4 system ID (0 = unused).</summary>
    public byte Neighbor4Id { get; init; }

    /// <summary>Neighbour 4 RSSI.</summary>
    public byte Neighbor4Rssi { get; init; }

    /// <summary>Neighbour 5 system ID (0 = unused).</summary>
    public byte Neighbor5Id { get; init; }

    /// <summary>Neighbour 5 RSSI.</summary>
    public byte Neighbor5Rssi { get; init; }

    /// <summary>Whether this drone has a ground link: 0=No, 1=Yes.</summary>
    public byte HasGroundLink { get; init; }

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimestampMs);
        buffer[8]  = ReporterSystemId;
        buffer[9]  = NeighborCount;
        buffer[10] = Neighbor1Id;
        buffer[11] = Neighbor1Rssi;
        buffer[12] = Neighbor2Id;
        buffer[13] = Neighbor2Rssi;
        buffer[14] = Neighbor3Id;
        buffer[15] = Neighbor3Rssi;
        buffer[16] = Neighbor4Id;
        buffer[17] = Neighbor4Rssi;
        buffer[18] = Neighbor5Id;
        buffer[19] = Neighbor5Rssi;
        buffer[20] = HasGroundLink;
        buffer[21] = 0; // reserved / alignment
    }

    /// <summary>Deserializes a <see cref="ResqMeshTopology"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="ResqMeshTopology"/>.</returns>
    public static ResqMeshTopology Deserialize(ReadOnlySpan<byte> buffer) => new()
    {
        TimestampMs = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
        ReporterSystemId = buffer[8],
        NeighborCount = buffer[9],
        Neighbor1Id = buffer[10],
        Neighbor1Rssi = buffer[11],
        Neighbor2Id = buffer[12],
        Neighbor2Rssi = buffer[13],
        Neighbor3Id = buffer[14],
        Neighbor3Rssi = buffer[15],
        Neighbor4Id = buffer[16],
        Neighbor4Rssi = buffer[17],
        Neighbor5Id = buffer[18],
        Neighbor5Rssi = buffer[19],
        HasGroundLink = buffer[20],
    };
}
