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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Dialect.Messages;

namespace ResQ.Mavlink.Mesh;

/// <summary>
/// Represents a single direct neighbour drone observed via heartbeat.
/// </summary>
public sealed class MeshNeighborEntry
{
    /// <summary>Gets the MAVLink system ID of this neighbour.</summary>
    public byte SystemId { get; init; }

    /// <summary>Gets or sets the received signal strength indicator (dBm convention).</summary>
    public int Rssi { get; set; }

    /// <summary>Gets or sets the timestamp of the last received heartbeat.</summary>
    public DateTimeOffset LastSeen { get; set; }

    /// <summary>Gets or sets whether this neighbour reports a ground link.</summary>
    public bool HasGroundLink { get; set; }

    /// <summary>
    /// Returns whether this entry has not been refreshed within the given timeout.
    /// </summary>
    /// <param name="timeoutSec">Stale threshold in seconds.</param>
    public bool IsStale(int timeoutSec) => DateTimeOffset.UtcNow - LastSeen > TimeSpan.FromSeconds(timeoutSec);
}

/// <summary>
/// Tracks direct mesh neighbours observed via heartbeat packets and builds
/// RESQ_MESH_TOPOLOGY messages for broadcast.
/// </summary>
public sealed class MeshNeighborTable
{
    private readonly MeshNeighborTableOptions _options;
    private readonly Dictionary<byte, MeshNeighborEntry> _entries = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initialises a new <see cref="MeshNeighborTable"/> with the supplied options.
    /// </summary>
    /// <param name="options">Table configuration.</param>
    public MeshNeighborTable(IOptions<MeshNeighborTableOptions> options)
        => _options = options.Value;

    /// <summary>
    /// Updates an existing neighbour record or inserts a new one.
    /// </summary>
    /// <param name="systemId">MAVLink system ID of the neighbour.</param>
    /// <param name="rssi">Received signal strength in dBm.</param>
    /// <param name="hasGroundLink">Whether the neighbour reports a ground link.</param>
    public void Update(byte systemId, int rssi, bool hasGroundLink)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(systemId, out var entry))
            {
                entry = new MeshNeighborEntry { SystemId = systemId };
                _entries[systemId] = entry;
            }
            entry.Rssi = rssi;
            entry.HasGroundLink = hasGroundLink;
            entry.LastSeen = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Returns all non-stale neighbours.
    /// </summary>
    public IReadOnlyList<MeshNeighborEntry> GetNeighbors()
    {
        lock (_lock)
            return _entries.Values.Where(e => !e.IsStale(_options.NeighborTimeoutSec)).ToList();
    }

    /// <summary>
    /// Returns all neighbours including stale ones.
    /// </summary>
    public IReadOnlyList<MeshNeighborEntry> GetAllNeighbors()
    {
        lock (_lock)
            return _entries.Values.ToList();
    }

    /// <summary>
    /// Gets whether the mesh is partitioned from the ground station —
    /// i.e. no active neighbour has a ground link.
    /// </summary>
    public bool IsPartitioned
    {
        get
        {
            lock (_lock)
                return !_entries.Values.Any(e => !e.IsStale(_options.NeighborTimeoutSec) && e.HasGroundLink);
        }
    }

    /// <summary>
    /// Builds a <see cref="ResqMeshTopology"/> message from the current (non-stale) neighbour state.
    /// Includes up to five neighbours ordered by descending RSSI.
    /// </summary>
    /// <param name="ownSystemId">The system ID of the drone building this topology report.</param>
    /// <returns>A populated <see cref="ResqMeshTopology"/> ready for serialisation.</returns>
    public ResqMeshTopology BuildTopologyMessage(byte ownSystemId)
    {
        IReadOnlyList<MeshNeighborEntry> neighbors;
        bool hasGroundLink;
        lock (_lock)
        {
            neighbors = _entries.Values
                .Where(e => !e.IsStale(_options.NeighborTimeoutSec))
                .OrderByDescending(e => e.Rssi)
                .Take(5)
                .ToList();
            hasGroundLink = _entries.Values.Any(e => !e.IsStale(_options.NeighborTimeoutSec) && e.HasGroundLink);
        }

        var count = (byte)neighbors.Count;

        return new ResqMeshTopology
        {
            TimestampMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ReporterSystemId = ownSystemId,
            NeighborCount = count,
            Neighbor1Id = count > 0 ? neighbors[0].SystemId : (byte)0,
            Neighbor1Rssi = count > 0 ? (byte)(neighbors[0].Rssi & 0xFF) : (byte)0,
            Neighbor2Id = count > 1 ? neighbors[1].SystemId : (byte)0,
            Neighbor2Rssi = count > 1 ? (byte)(neighbors[1].Rssi & 0xFF) : (byte)0,
            Neighbor3Id = count > 2 ? neighbors[2].SystemId : (byte)0,
            Neighbor3Rssi = count > 2 ? (byte)(neighbors[2].Rssi & 0xFF) : (byte)0,
            Neighbor4Id = count > 3 ? neighbors[3].SystemId : (byte)0,
            Neighbor4Rssi = count > 3 ? (byte)(neighbors[3].Rssi & 0xFF) : (byte)0,
            Neighbor5Id = count > 4 ? neighbors[4].SystemId : (byte)0,
            Neighbor5Rssi = count > 4 ? (byte)(neighbors[4].Rssi & 0xFF) : (byte)0,
            HasGroundLink = hasGroundLink ? (byte)1 : (byte)0,
        };
    }
}
