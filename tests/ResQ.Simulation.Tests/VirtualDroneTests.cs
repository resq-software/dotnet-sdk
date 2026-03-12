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
using ResQ.Clients;
using ResQ.Simulation;
using Location = ResQ.Core.Location;
using Xunit;

namespace ResQ.Simulation.Tests;

public class VirtualDroneTests
{
    private readonly CoordinationHceClient _hce = new("http://localhost:3000");
    private readonly InfrastructureApiClient _infra = new("http://localhost:5000");

    [Fact]
    public void Constructor_WithValidParams_ShouldNotThrow()
    {
        var location = new Location(37.7749, -122.4194, 50.0);
        var act = () => new VirtualDrone("drone-1", location, _hce, _infra);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullDroneId_ShouldThrow()
    {
        var location = new Location(37.7749, -122.4194, 50.0);
        var act = () => new VirtualDrone(null!, location, _hce, _infra);
        act.Should().Throw<ArgumentNullException>().WithParameterName("droneId");
    }

    [Fact]
    public void Constructor_WithEmptyDroneId_ShouldThrow()
    {
        var location = new Location(37.7749, -122.4194, 50.0);
        var act = () => new VirtualDrone("", location, _hce, _infra);
        act.Should().Throw<ArgumentException>().WithParameterName("droneId");
    }

    [Fact]
    public void Constructor_WithWhitespaceDroneId_ShouldThrow()
    {
        var location = new Location(37.7749, -122.4194, 50.0);
        var act = () => new VirtualDrone("   ", location, _hce, _infra);
        act.Should().Throw<ArgumentException>().WithParameterName("droneId");
    }

    [Fact]
    public void Constructor_WithNullLocation_ShouldThrow()
    {
        var act = () => new VirtualDrone("drone-1", null!, _hce, _infra);
        act.Should().Throw<ArgumentNullException>().WithParameterName("startLocation");
    }

    [Fact]
    public void Constructor_WithNullHce_ShouldThrow()
    {
        var location = new Location(37.7749, -122.4194, 50.0);
        var act = () => new VirtualDrone("drone-1", location, null!, _infra);
        act.Should().Throw<ArgumentNullException>().WithParameterName("hce");
    }

    [Fact]
    public void Constructor_WithNullInfra_ShouldThrow()
    {
        var location = new Location(37.7749, -122.4194, 50.0);
        var act = () => new VirtualDrone("drone-1", location, _hce, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("infra");
    }

    [Theory]
    [InlineData(-91.0, 0.0, 50.0)]
    [InlineData(91.0, 0.0, 50.0)]
    public void Constructor_WithInvalidLatitude_ShouldThrow(double lat, double lon, double alt)
    {
        var location = new Location(lat, lon, alt);
        var act = () => new VirtualDrone("drone-1", location, _hce, _infra);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0.0, -181.0, 50.0)]
    [InlineData(0.0, 181.0, 50.0)]
    public void Constructor_WithInvalidLongitude_ShouldThrow(double lat, double lon, double alt)
    {
        var location = new Location(lat, lon, alt);
        var act = () => new VirtualDrone("drone-1", location, _hce, _infra);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0.0, 0.0, 5.0)]   // Below MIN_ALTITUDE (10m)
    [InlineData(0.0, 0.0, 121.0)] // Above MAX_ALTITUDE (120m)
    public void Constructor_WithInvalidAltitude_ShouldThrow(double lat, double lon, double alt)
    {
        var location = new Location(lat, lon, alt);
        var act = () => new VirtualDrone("drone-1", location, _hce, _infra);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithBoundaryValidAltitude_ShouldNotThrow()
    {
        var lowAlt = new Location(0.0, 0.0, 10.0);  // MIN_ALTITUDE
        var highAlt = new Location(0.0, 0.0, 120.0); // MAX_ALTITUDE

        var act1 = () => new VirtualDrone("drone-1", lowAlt, _hce, _infra);
        var act2 = () => new VirtualDrone("drone-2", highAlt, _hce, _infra);

        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    [Fact]
    public void Location_Record_Equality()
    {
        var a = new Location(37.7749, -122.4194, 50.0);
        var b = new Location(37.7749, -122.4194, 50.0);
        a.Should().Be(b);
    }

    [Fact]
    public void Location_Record_Inequality()
    {
        var a = new Location(37.7749, -122.4194, 50.0);
        var b = new Location(37.7750, -122.4194, 50.0);
        a.Should().NotBe(b);
    }
}
