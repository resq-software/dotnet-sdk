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

using System;
using System.Numerics;
using FluentAssertions;
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Physics;

/// <summary>
/// Unit tests for <see cref="QuadrotorFlightModel"/> using a 6DOF physics model
/// with PD control and aerodynamic forces.
/// </summary>
public class QuadrotorFlightModelTests
{
    private const double Mass = 2.0;
    private static readonly Vector3 StartAt50m = new(0f, 50f, 0f);
    private static readonly Vector3 NoWind = Vector3.Zero;

    // 1. Constructor sets initial state: pos, velocity=zero, battery=100, not landed
    [Fact]
    public void Constructor_SetsInitialState()
    {
        var model = new QuadrotorFlightModel(StartAt50m, Mass);

        model.State.Position.Should().Be(StartAt50m);
        model.State.Velocity.Should().Be(Vector3.Zero);
        model.State.BatteryPercent.Should().BeApproximately(100.0, 1e-10);
        model.HasLanded.Should().BeFalse();
        model.LaunchPosition.Should().Be(StartAt50m);
    }

    // 2. Hover maintains altitude within tolerance — hover for 2 sec, altitude within 5m of start (50m)
    [Fact]
    public void Hover_MaintainsAltitude_WithinTolerance()
    {
        var model = new QuadrotorFlightModel(StartAt50m, Mass);
        model.ApplyCommand(FlightCommand.Hover());

        // Simulate 2 seconds with 0.02s timestep
        for (var i = 0; i < 100; i++)
            model.Step(0.02, NoWind);

        model.State.Position.Y.Should().BeApproximately(50f, 5f);
    }

    // 3. No thrust / land command: after 1 sec, altitude < 50
    [Fact]
    public void NoThrust_FallsDueToGravity()
    {
        var model = new QuadrotorFlightModel(StartAt50m, Mass);
        model.ApplyCommand(FlightCommand.Land());

        // Simulate 1 second with 0.02s timestep
        for (var i = 0; i < 50; i++)
            model.Step(0.02, NoWind);

        model.State.Position.Y.Should().BeLessThan(50f);
    }

    // 4. Wind affects position — hover with strong east wind (20,0,0), after 1sec, X > 0
    [Fact]
    public void Wind_AffectsPosition()
    {
        var model = new QuadrotorFlightModel(StartAt50m, Mass);
        model.ApplyCommand(FlightCommand.Hover());
        var eastWind = new Vector3(20f, 0f, 0f);

        for (var i = 0; i < 50; i++)
            model.Step(0.02, eastWind);

        model.State.Position.X.Should().BeGreaterThan(0.0f);
    }

    // 5. GoToWaypoint moves toward target — GoTo (50,50,0), after 5sec, X > 0
    [Fact]
    public void GoToWaypoint_MovesTowardTarget()
    {
        var model = new QuadrotorFlightModel(StartAt50m, Mass);
        model.ApplyCommand(FlightCommand.GoTo(new Vector3(50f, 50f, 0f)));

        // Simulate 5 seconds with 0.02s timestep
        for (var i = 0; i < 250; i++)
            model.Step(0.02, NoWind);

        model.State.Position.X.Should().BeGreaterThan(0.0f);
    }

    // 6. Battery drains proportional to thrust — hover 1sec, battery < 100
    [Fact]
    public void BatteryDrains_ProportionalToThrust()
    {
        var model = new QuadrotorFlightModel(StartAt50m, Mass);
        model.ApplyCommand(FlightCommand.Hover());

        for (var i = 0; i < 50; i++)
            model.Step(0.02, NoWind);

        model.State.BatteryPercent.Should().BeLessThan(100.0);
    }

    // 7. Constructor with invalid mass (0) throws ArgumentOutOfRangeException
    [Fact]
    public void Constructor_InvalidMass_Throws()
    {
        var act = () => new QuadrotorFlightModel(StartAt50m, 0.0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
