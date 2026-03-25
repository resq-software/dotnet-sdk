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

using FluentAssertions;
using ResQ.Simulation.Engine.Environment;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Environment;

public class HeightmapTerrainTests
{
    // Helper: 2×2 grid with known corner values
    //   [0,0]=0  [0,1]=10
    //   [1,0]=20 [1,1]=30
    private static HeightmapTerrain MakeSimple2x2(double width = 100, double depth = 100)
    {
        var heights = new float[2, 2]
        {
            { 0f, 10f },
            { 20f, 30f },
        };
        return new HeightmapTerrain(heights, width, depth);
    }

    // 1. GetElevation returns interpolated height at the centre of the grid
    [Fact]
    public void GetElevation_CentreOfGrid_ReturnsBilinearInterpolation()
    {
        var terrain = MakeSimple2x2();

        // Centre = (x=50, z=50) → fraction (0.5, 0.5) in the grid
        // bilinear: 0*(0.5)*(0.5) + 10*(0.5)*(0.5) + 20*(0.5)*(0.5) + 30*(0.5)*(0.5)
        //         = 0.25*(0+10+20+30) = 15
        double elevation = terrain.GetElevation(50, 50);

        elevation.Should().BeApproximately(15.0, 1e-6);
    }

    // 2. GetElevation at grid corners returns exact heightmap values
    [Theory]
    [InlineData(0, 0, 0f)]
    [InlineData(100, 0, 10f)]
    [InlineData(0, 100, 20f)]
    [InlineData(100, 100, 30f)]
    public void GetElevation_AtCorners_ReturnsExactValues(double x, double z, double expected)
    {
        var terrain = MakeSimple2x2();

        terrain.GetElevation(x, z).Should().BeApproximately(expected, 1e-5);
    }

    // 3. Out-of-bounds X (negative) clamps to left edge
    [Fact]
    public void GetElevation_NegativeX_ClampsToLeftEdge()
    {
        var terrain = MakeSimple2x2();

        // x=-50 should clamp to x=0, z=0 → elevation 0
        terrain.GetElevation(-50, 0).Should().BeApproximately(0.0, 1e-5);
    }

    // 4. Out-of-bounds X (beyond width) clamps to right edge
    [Fact]
    public void GetElevation_XBeyondWidth_ClampsToRightEdge()
    {
        var terrain = MakeSimple2x2();

        // x=200 clamps to x=100, z=0 → elevation 10
        terrain.GetElevation(200, 0).Should().BeApproximately(10.0, 1e-5);
    }

    // 5. Empty heightmap throws ArgumentException
    [Fact]
    public void Constructor_EmptyHeightmap_ThrowsArgumentException()
    {
        var empty = new float[0, 0];

        var act = () => new HeightmapTerrain(empty, 100, 100);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("heights");
    }

    // 6. Zero width throws ArgumentException
    [Fact]
    public void Constructor_ZeroWidth_ThrowsArgumentException()
    {
        var heights = new float[2, 2] { { 0f, 1f }, { 2f, 3f } };

        var act = () => new HeightmapTerrain(heights, width: 0, depth: 100);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("width");
    }

    // 7. GetSurfaceType returns Vegetation by default (no surface map supplied)
    [Fact]
    public void GetSurfaceType_NoSurfaceMap_ReturnsVegetation()
    {
        var terrain = MakeSimple2x2();

        terrain.GetSurfaceType(50, 50).Should().Be(SurfaceType.Vegetation);
    }

    // 8. GetSurfaceType returns the correct type from the surface map
    [Fact]
    public void GetSurfaceType_WithSurfaceMap_ReturnsCorrectType()
    {
        var heights = new float[2, 2] { { 0f, 0f }, { 0f, 0f } };
        var surfaceMap = new SurfaceType[2, 2]
        {
            { SurfaceType.Vegetation, SurfaceType.Water },
            { SurfaceType.Urban, SurfaceType.BareGround },
        };
        var terrain = new HeightmapTerrain(heights, 100, 100, surfaceMap);

        // Top-right cell (col=1, row=0) → Water; query at x=100, z=0
        terrain.GetSurfaceType(100, 0).Should().Be(SurfaceType.Water);

        // Bottom-left cell (col=0, row=1) → Urban; query at x=0, z=100
        terrain.GetSurfaceType(0, 100).Should().Be(SurfaceType.Urban);
    }

    // 9. Width and Depth return the configured values
    [Fact]
    public void WidthAndDepth_ReturnConfiguredValues()
    {
        var terrain = MakeSimple2x2(width: 500, depth: 250);

        terrain.Width.Should().Be(500);
        terrain.Depth.Should().Be(250);
    }
}
