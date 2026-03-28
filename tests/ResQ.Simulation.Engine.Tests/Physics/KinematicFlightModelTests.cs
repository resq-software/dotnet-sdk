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
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Physics;

public class KinematicFlightModelTests
{
    private static readonly Vector3 NoWind = Vector3.Zero;

    // 1. Constructor sets initial state position correctly
    [Fact]
    public void Constructor_SetsInitialStateAtStartPosition()
    {
        var start = new Vector3(10f, 20f, 30f);
        var model = new KinematicFlightModel(start);

        model.State.Position.Should().Be(start);
        model.State.BatteryPercent.Should().BeApproximately(100.0, 1e-10);
        model.LaunchPosition.Should().Be(start);
        model.HasLanded.Should().BeFalse();
    }

    // 2. Hover command: position stays in place after a step
    [Fact]
    public void Step_HoverCommand_PositionDoesNotChange()
    {
        var start = new Vector3(0f, 50f, 0f);
        var model = new KinematicFlightModel(start);
        model.ApplyCommand(FlightCommand.Hover());

        model.Step(1.0, NoWind);

        model.State.Position.Should().Be(start);
    }

    // 3. GoTo command: position moves toward the target
    [Fact]
    public void Step_GoToCommand_MovesDroneTowardTarget()
    {
        var start = new Vector3(0f, 10f, 0f);
        var target = new Vector3(100f, 10f, 0f);
        var model = new KinematicFlightModel(start);
        model.ApplyCommand(FlightCommand.GoTo(target));

        model.Step(1.0, NoWind);

        // Drone should have moved in the +X direction
        model.State.Position.X.Should().BeGreaterThan(start.X);
        model.State.Position.Y.Should().BeApproximately(start.Y, 0.01f);
        model.State.Position.Z.Should().BeApproximately(start.Z, 0.01f);
    }

    // 4. GoTo command: drone stops when it reaches the target (within threshold)
    [Fact]
    public void Step_GoToCommand_StopsWhenWaypointReached()
    {
        var start = new Vector3(0f, 10f, 0f);
        var target = new Vector3(0.5f, 10f, 0f); // well within 1m threshold
        var model = new KinematicFlightModel(start);
        model.ApplyCommand(FlightCommand.GoTo(target));

        model.Step(1.0, NoWind);

        // Position should not overshoot — velocity is zero when within threshold
        model.State.Position.X.Should().BeApproximately(start.X, 0.001f);
    }

    // 5. Wind offsets position even while hovering
    [Fact]
    public void Step_WithWind_OffsetsDronePosition()
    {
        var start = new Vector3(0f, 50f, 0f);
        var wind = new Vector3(5f, 0f, 0f); // 5 m/s eastward
        var model = new KinematicFlightModel(start);
        model.ApplyCommand(FlightCommand.Hover());

        model.Step(1.0, wind);

        model.State.Position.X.Should().BeApproximately(5f, 0.001f);
    }

    // 6. Battery drains during flight steps
    [Fact]
    public void Step_NormalFlight_DrainsBattery()
    {
        var model = new KinematicFlightModel(new Vector3(0f, 50f, 0f));
        model.ApplyCommand(FlightCommand.Hover());

        model.Step(1.0, NoWind);

        model.State.BatteryPercent.Should().BeLessThan(100.0);
    }

    // 7. Land command: altitude decreases over time
    [Fact]
    public void Step_LandCommand_AltitudeDecreases()
    {
        var start = new Vector3(0f, 20f, 0f);
        var model = new KinematicFlightModel(start);
        model.ApplyCommand(FlightCommand.Land());

        model.Step(1.0, NoWind);

        model.State.Position.Y.Should().BeLessThan(start.Y);
    }

    // 8. Land command: HasLanded becomes true when altitude reaches threshold
    [Fact]
    public void Step_LandCommand_SetsHasLandedWhenOnGround()
    {
        // Start at low altitude just above the landed threshold
        var start = new Vector3(0f, 0.3f, 0f);
        var model = new KinematicFlightModel(start);
        model.ApplyCommand(FlightCommand.Land());

        // A single 1-second step at 2 m/s landing speed will push Y <= 0 → clamped to 0
        model.Step(1.0, NoWind);

        model.HasLanded.Should().BeTrue();
    }

    // 9. RTL command: drone moves toward launch position
    [Fact]
    public void Step_RTLCommand_MovesDroneTowardLaunchPosition()
    {
        var launch = new Vector3(0f, 10f, 0f);
        var model = new KinematicFlightModel(launch);

        // Teleport the drone away by issuing a GoTo and stepping many times
        model.ApplyCommand(FlightCommand.GoTo(new Vector3(100f, 10f, 0f)));
        for (var i = 0; i < 20; i++)
            model.Step(1.0, NoWind);

        var positionAfterGoTo = model.State.Position;

        model.ApplyCommand(FlightCommand.RTL());
        model.Step(1.0, NoWind);

        // X distance to launch should have decreased
        var distBefore = Math.Abs(positionAfterGoTo.X - launch.X);
        var distAfter = Math.Abs(model.State.Position.X - launch.X);
        distAfter.Should().BeLessThan(distBefore);
    }

    // 10. Maximum speed is respected when navigating to a distant waypoint
    [Fact]
    public void Step_GoToDistantTarget_DoesNotExceedMaxSpeed()
    {
        var maxSpeed = 15.0;
        var start = new Vector3(0f, 10f, 0f);
        var target = new Vector3(10_000f, 10f, 0f);
        var model = new KinematicFlightModel(start, maxSpeed);
        model.ApplyCommand(FlightCommand.GoTo(target));

        model.Step(1.0, NoWind);

        var displacement = Vector3.Distance(model.State.Position, start);
        displacement.Should().BeApproximately((float)maxSpeed, 0.001f);
    }
}
