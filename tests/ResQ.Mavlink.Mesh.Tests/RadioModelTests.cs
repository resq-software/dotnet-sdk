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
using FluentAssertions;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Mesh.Simulation;
using ResQ.Simulation.Engine.Environment;
using Xunit;

namespace ResQ.Mavlink.Mesh.Tests;

/// <summary>
/// Unit tests for <see cref="RadioModel"/>.
/// </summary>
public sealed class RadioModelTests
{
    private static RadioModel CreateRadioModel(RadioModelOptions? opts = null)
        => new(Options.Create(opts ?? new RadioModelOptions { MaxRangeMetres = 500f, AttenuationFactor = 2f, BasePacketLossPercent = 0f }));

    private static readonly Vector3 Origin = Vector3.Zero;

    [Fact]
    public void CanCommunicate_WithinRange_ReturnsTrue()
    {
        var radio = CreateRadioModel();
        radio.CanCommunicate(Origin, new Vector3(100, 0, 0)).Should().BeTrue();
    }

    [Fact]
    public void CanCommunicate_BeyondRange_ReturnsFalse()
    {
        var radio = CreateRadioModel();
        radio.CanCommunicate(Origin, new Vector3(600, 0, 0)).Should().BeFalse();
    }

    [Fact]
    public void CanCommunicate_ExactlyAtRange_ReturnsTrue()
    {
        var radio = CreateRadioModel();
        radio.CanCommunicate(Origin, new Vector3(500, 0, 0)).Should().BeTrue();
    }

    [Fact]
    public void GetSignalStrength_AtZeroDistance_ReturnsOne()
    {
        var radio = CreateRadioModel();
        radio.GetSignalStrength(Origin, Origin).Should().BeApproximately(1.0f, 0.001f);
    }

    [Fact]
    public void GetSignalStrength_AtHalfRange_IsApproximatelyQuarter()
    {
        // At half range: normalised = 0.5, strength = (1 - 0.5)^2 = 0.25
        var radio = CreateRadioModel();
        var halfRange = new Vector3(250, 0, 0);
        radio.GetSignalStrength(Origin, halfRange).Should().BeApproximately(0.25f, 0.01f);
    }

    [Fact]
    public void GetSignalStrength_BeyondRange_ReturnsZero()
    {
        var radio = CreateRadioModel();
        radio.GetSignalStrength(Origin, new Vector3(600, 0, 0)).Should().Be(0f);
    }

    [Fact]
    public void CanCommunicateWithLos_FlatTerrain_WithinRange_ReturnsTrue()
    {
        var radio = CreateRadioModel();
        var terrain = new FlatTerrain(elevation: 0.0);
        // Both drones at height 50m, within range
        var a = new Vector3(0, 50, 0);
        var b = new Vector3(100, 50, 0);
        radio.CanCommunicateWithLos(a, b, terrain).Should().BeTrue();
    }

    [Fact]
    public void CanCommunicateWithLos_BeyondRange_ReturnsFalse()
    {
        var radio = CreateRadioModel();
        var terrain = new FlatTerrain(elevation: 0.0);
        var a = new Vector3(0, 50, 0);
        var b = new Vector3(700, 50, 0); // beyond 500m range
        radio.CanCommunicateWithLos(a, b, terrain).Should().BeFalse();
    }

    [Fact]
    public void CanCommunicateWithLos_MountainBetween_ReturnsFalse()
    {
        var radio = CreateRadioModel();
        // Terrain with a 200m peak at midpoint; drones at 50m
        var terrain = new MidpointMountainTerrain(midpointElevation: 200.0);
        var a = new Vector3(0, 50, 0);
        var b = new Vector3(200, 50, 0);
        radio.CanCommunicateWithLos(a, b, terrain).Should().BeFalse();
    }

    [Fact]
    public void ShouldDropPacket_BeyondRange_AlwaysDrops()
    {
        var radio = CreateRadioModel();
        // Run multiple times — should always drop when beyond range
        for (var i = 0; i < 20; i++)
            radio.ShouldDropPacket(Origin, new Vector3(600, 0, 0)).Should().BeTrue();
    }

    [Fact]
    public void ShouldDropPacket_AtZeroDistance_RarelyDrops()
    {
        // With BasePacketLossPercent=1.0 and distance=0, loss=1%
        var radio = CreateRadioModel(new RadioModelOptions
        { MaxRangeMetres = 500f, AttenuationFactor = 2f, BasePacketLossPercent = 0f });
        var drops = 0;
        for (var i = 0; i < 10000; i++)
            if (radio.ShouldDropPacket(Origin, Origin)) drops++;

        // With 0% base loss at zero distance, no drops expected
        drops.Should().Be(0);
    }

    // ── stub terrain implementations ─────────────────────────────────────

    private sealed class FlatTerrain(double elevation) : ITerrain
    {
        public double Width => 10000;
        public double Depth => 10000;
        public double GetElevation(double x, double z) => elevation;
        public SurfaceType GetSurfaceType(double x, double z) => SurfaceType.Vegetation;
    }

    private sealed class MidpointMountainTerrain(double midpointElevation) : ITerrain
    {
        public double Width => 10000;
        public double Depth => 10000;
        public double GetElevation(double x, double z) => midpointElevation;
        public SurfaceType GetSurfaceType(double x, double z) => SurfaceType.BareGround;
    }
}
