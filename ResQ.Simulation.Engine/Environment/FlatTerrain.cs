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
/// A uniform flat terrain that returns a constant elevation and surface type for
/// every position query.  Suitable for simple test scenarios and urban environments
/// where ground variation can be ignored.
/// </summary>
public sealed class FlatTerrain : ITerrain
{
    private readonly double _elevation;
    private readonly SurfaceType _surfaceType;

    /// <summary>
    /// Initialises a new <see cref="FlatTerrain"/> with the specified dimensions,
    /// elevation, and surface type.
    /// </summary>
    /// <param name="width">
    /// Terrain width along the X axis in metres.  Defaults to <c>1000</c>.
    /// </param>
    /// <param name="depth">
    /// Terrain depth along the Z axis in metres.  Defaults to <c>1000</c>.
    /// </param>
    /// <param name="elevation">
    /// Constant elevation returned for all positions in metres.  Defaults to <c>0</c>.
    /// </param>
    /// <param name="surfaceType">
    /// Surface type returned for all positions.  Defaults to <see cref="SurfaceType.Vegetation"/>.
    /// </param>
    public FlatTerrain(
        double width = 1000,
        double depth = 1000,
        double elevation = 0,
        SurfaceType surfaceType = SurfaceType.Vegetation)
    {
        Width = width;
        Depth = depth;
        _elevation = elevation;
        _surfaceType = surfaceType;
    }

    /// <inheritdoc/>
    public double Width { get; }

    /// <inheritdoc/>
    public double Depth { get; }

    /// <summary>
    /// Returns the constant elevation configured at construction, regardless of position.
    /// </summary>
    /// <param name="x">The X (East) coordinate in metres (ignored).</param>
    /// <param name="z">The Z (South) coordinate in metres (ignored).</param>
    /// <returns>The configured constant elevation in metres.</returns>
    public double GetElevation(double x, double z) => _elevation;

    /// <summary>
    /// Returns the constant surface type configured at construction, regardless of position.
    /// </summary>
    /// <param name="x">The X (East) coordinate in metres (ignored).</param>
    /// <param name="z">The Z (South) coordinate in metres (ignored).</param>
    /// <returns>The configured <see cref="SurfaceType"/>.</returns>
    public SurfaceType GetSurfaceType(double x, double z) => _surfaceType;
}
