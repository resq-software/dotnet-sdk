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

using System.Numerics;
using Microsoft.Extensions.Options;
using ResQ.Simulation.Engine.Environment;

namespace ResQ.Mavlink.Mesh.Simulation;

/// <summary>
/// Simulates radio link constraints between two positions in the simulation world.
/// Used when real radio hardware is not available (e.g., SITL).
/// </summary>
public sealed class RadioModel
{
    private readonly RadioModelOptions _options;
    private readonly Random _rng = new();

    /// <summary>
    /// Initialises a new <see cref="RadioModel"/> with the supplied options.
    /// </summary>
    /// <param name="options">Radio model configuration.</param>
    public RadioModel(IOptions<RadioModelOptions> options)
        => _options = options.Value;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="posA"/> and <paramref name="posB"/>
    /// are within the configured maximum range.
    /// </summary>
    /// <param name="posA">Position of node A in world space (metres).</param>
    /// <param name="posB">Position of node B in world space (metres).</param>
    public bool CanCommunicate(Vector3 posA, Vector3 posB)
        => Vector3.Distance(posA, posB) <= _options.MaxRangeMetres;

    /// <summary>
    /// Returns <see langword="true"/> if the two nodes are within range AND have line-of-sight.
    /// Line-of-sight is approximated by checking whether the terrain elevation at the midpoint
    /// exceeds the height of the straight line between the two positions at that midpoint.
    /// </summary>
    /// <param name="posA">Position of node A in world space (metres).</param>
    /// <param name="posB">Position of node B in world space (metres).</param>
    /// <param name="terrain">Terrain provider for elevation queries.</param>
    public bool CanCommunicateWithLos(Vector3 posA, Vector3 posB, ITerrain terrain)
    {
        if (!CanCommunicate(posA, posB)) return false;

        // Sample the midpoint
        var mid = (posA + posB) * 0.5f;
        var terrainHeight = terrain.GetElevation(mid.X, mid.Z);
        // Height of the straight line between A and B at the midpoint is the average Y
        var lineHeightAtMid = (posA.Y + posB.Y) * 0.5f;
        return terrainHeight <= lineHeightAtMid;
    }

    /// <summary>
    /// Returns the normalised signal strength between 0.0 and 1.0 using an inverse-power-law
    /// decay with the configured attenuation exponent.
    /// Returns 1.0 when the two positions are coincident.
    /// </summary>
    /// <param name="posA">Position of node A.</param>
    /// <param name="posB">Position of node B.</param>
    public float GetSignalStrength(Vector3 posA, Vector3 posB)
    {
        var distance = Vector3.Distance(posA, posB);
        if (distance <= 0f) return 1f;
        if (distance > _options.MaxRangeMetres) return 0f;

        // Normalise distance to [0,1] relative to max range, then apply inverse power law.
        var normalised = distance / _options.MaxRangeMetres;
        return MathF.Pow(1f - normalised, _options.AttenuationFactor);
    }

    /// <summary>
    /// Returns <see langword="true"/> if a packet should be dropped due to distance-based
    /// probabilistic loss.  Loss probability increases linearly with distance beyond zero.
    /// </summary>
    /// <param name="posA">Position of node A.</param>
    /// <param name="posB">Position of node B.</param>
    public bool ShouldDropPacket(Vector3 posA, Vector3 posB)
    {
        var distance = Vector3.Distance(posA, posB);
        if (distance > _options.MaxRangeMetres) return true;

        var distanceFraction = _options.MaxRangeMetres > 0
            ? distance / _options.MaxRangeMetres
            : 0f;

        // Loss% = base + (100 - base) * distanceFraction^2
        var lossPercent = _options.BasePacketLossPercent
            + (100f - _options.BasePacketLossPercent) * distanceFraction * distanceFraction;

        return _rng.NextDouble() * 100.0 < lossPercent;
    }
}
