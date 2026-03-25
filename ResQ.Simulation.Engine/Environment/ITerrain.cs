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

namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// Provides spatial elevation and surface-type queries over a simulation terrain.
/// </summary>
/// <remarks>
/// Coordinates use a right-handed, Y-up world space where X is the East axis and
/// Z is the South axis.  Elevation is returned along the Y axis in metres above
/// an arbitrary datum.
/// </remarks>
public interface ITerrain
{
    /// <summary>
    /// Gets the terrain width in metres along the X axis.
    /// </summary>
    double Width { get; }

    /// <summary>
    /// Gets the terrain depth in metres along the Z axis.
    /// </summary>
    double Depth { get; }

    /// <summary>
    /// Returns the terrain elevation at world-space coordinates
    /// (<paramref name="x"/>, <paramref name="z"/>) in metres.
    /// </summary>
    /// <param name="x">The X (East) coordinate in metres.</param>
    /// <param name="z">The Z (South) coordinate in metres.</param>
    /// <returns>Elevation in metres above the terrain datum.</returns>
    double GetElevation(double x, double z);

    /// <summary>
    /// Returns the surface type at world-space coordinates
    /// (<paramref name="x"/>, <paramref name="z"/>).
    /// </summary>
    /// <param name="x">The X (East) coordinate in metres.</param>
    /// <param name="z">The Z (South) coordinate in metres.</param>
    /// <returns>The <see cref="SurfaceType"/> at the specified location.</returns>
    SurfaceType GetSurfaceType(double x, double z);
}
