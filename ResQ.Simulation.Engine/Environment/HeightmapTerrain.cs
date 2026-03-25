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
/// A terrain backed by a 2-D heightmap grid that uses bilinear interpolation to
/// produce smooth elevation values and nearest-neighbour lookup for surface types.
/// </summary>
/// <remarks>
/// The heightmap is indexed as <c>heights[row, col]</c> where row 0 corresponds to
/// Z = 0 and col 0 corresponds to X = 0.  World positions are mapped linearly to
/// fractional grid indices; out-of-bounds queries are clamped to the nearest edge.
/// </remarks>
public sealed class HeightmapTerrain : ITerrain
{
    private readonly float[,] _heights;
    private readonly SurfaceType[,]? _surfaceMap;
    private readonly int _rows;
    private readonly int _cols;

    /// <summary>
    /// Initialises a new <see cref="HeightmapTerrain"/> from a height grid and optional
    /// surface map.
    /// </summary>
    /// <param name="heights">
    /// 2-D array of elevation values in metres, indexed <c>[row, col]</c>.
    /// Must contain at least one element.
    /// </param>
    /// <param name="width">
    /// Terrain width along the X axis in metres.  Must be greater than zero.
    /// </param>
    /// <param name="depth">
    /// Terrain depth along the Z axis in metres.  Must be greater than zero.
    /// </param>
    /// <param name="surfaceMap">
    /// Optional 2-D array of surface types with the same dimensions as
    /// <paramref name="heights"/>.  When <see langword="null"/>, all positions
    /// default to <see cref="SurfaceType.Vegetation"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="heights"/> is empty, when
    /// <paramref name="width"/> or <paramref name="depth"/> is not positive, or when
    /// <paramref name="surfaceMap"/> dimensions do not match <paramref name="heights"/>.
    /// </exception>
    public HeightmapTerrain(
        float[,] heights,
        double width,
        double depth,
        SurfaceType[,]? surfaceMap = null)
    {
        _rows = heights.GetLength(0);
        _cols = heights.GetLength(1);

        if (_rows == 0 || _cols == 0)
            throw new ArgumentException("Heightmap must contain at least one element.", nameof(heights));

        if (width <= 0)
            throw new ArgumentException("Width must be greater than zero.", nameof(width));

        if (depth <= 0)
            throw new ArgumentException("Depth must be greater than zero.", nameof(depth));

        if (surfaceMap != null)
        {
            if (surfaceMap.GetLength(0) != _rows || surfaceMap.GetLength(1) != _cols)
                throw new ArgumentException(
                    "Surface map dimensions must match the heightmap dimensions.", nameof(surfaceMap));
        }

        _heights = heights;
        _surfaceMap = surfaceMap;
        Width = width;
        Depth = depth;
    }

    /// <inheritdoc/>
    public double Width { get; }

    /// <inheritdoc/>
    public double Depth { get; }

    /// <summary>
    /// Returns the terrain elevation at world position (<paramref name="x"/>, <paramref name="z"/>)
    /// using bilinear interpolation.  Positions outside the terrain bounds are clamped to the
    /// nearest edge sample.
    /// </summary>
    /// <param name="x">The X (East) coordinate in metres.</param>
    /// <param name="z">The Z (South) coordinate in metres.</param>
    /// <returns>Interpolated elevation in metres.</returns>
    public double GetElevation(double x, double z)
    {
        // Map world coords to fractional grid indices
        double fc = x / Width * (_cols - 1);
        double fr = z / Depth * (_rows - 1);

        // Clamp to valid range
        fc = Math.Clamp(fc, 0.0, _cols - 1);
        fr = Math.Clamp(fr, 0.0, _rows - 1);

        int c0 = (int)Math.Floor(fc);
        int r0 = (int)Math.Floor(fr);
        int c1 = Math.Min(c0 + 1, _cols - 1);
        int r1 = Math.Min(r0 + 1, _rows - 1);

        double tx = fc - c0;
        double tz = fr - r0;

        // Bilinear interpolation
        double h00 = _heights[r0, c0];
        double h10 = _heights[r1, c0];
        double h01 = _heights[r0, c1];
        double h11 = _heights[r1, c1];

        return h00 * (1 - tx) * (1 - tz)
             + h01 * tx * (1 - tz)
             + h10 * (1 - tx) * tz
             + h11 * tx * tz;
    }

    /// <summary>
    /// Returns the surface type at world position (<paramref name="x"/>, <paramref name="z"/>)
    /// using nearest-neighbour lookup.  Returns <see cref="SurfaceType.Vegetation"/> when no
    /// surface map was provided.  Positions outside the terrain bounds are clamped to the
    /// nearest edge sample.
    /// </summary>
    /// <param name="x">The X (East) coordinate in metres.</param>
    /// <param name="z">The Z (South) coordinate in metres.</param>
    /// <returns>The <see cref="SurfaceType"/> at the nearest grid cell.</returns>
    public SurfaceType GetSurfaceType(double x, double z)
    {
        if (_surfaceMap == null)
            return SurfaceType.Vegetation;

        double fc = x / Width * (_cols - 1);
        double fr = z / Depth * (_rows - 1);

        int col = (int)Math.Round(Math.Clamp(fc, 0.0, _cols - 1));
        int row = (int)Math.Round(Math.Clamp(fr, 0.0, _rows - 1));

        return _surfaceMap[row, col];
    }
}
