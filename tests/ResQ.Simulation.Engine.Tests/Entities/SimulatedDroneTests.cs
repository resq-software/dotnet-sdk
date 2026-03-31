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
using ResQ.Simulation.Engine.Core;
using ResQ.Simulation.Engine.Entities;
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Entities;

public class SimulatedDroneTests
{
    // 1. Constructor sets Id, initial position (via flight model), and initial battery.
    [Fact]
    public void Constructor_SetsProperties()
    {
        var start = new Vector3(10f, 0f, 20f);
        var drone = new SimulatedDrone("drone-1", start, FlightModelType.Kinematic);

        drone.Id.Should().Be("drone-1");
        drone.FlightModel.State.Position.Should().Be(start);
        drone.FlightModel.State.BatteryPercent.Should().BeApproximately(100.0, 1e-10);
    }

    // 2. Null / whitespace id throws ArgumentException.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceId_Throws(string? id)
    {
        var act = () => new SimulatedDrone(id!, Vector3.Zero, FlightModelType.Kinematic);
        act.Should().Throw<ArgumentException>();
    }

    // 3. Step delegates to the flight model — position changes after GoTo + Step.
    [Fact]
    public void Step_DelegatesToFlightModel()
    {
        var start = new Vector3(0f, 10f, 0f);
        var drone = new SimulatedDrone("d1", start, FlightModelType.Kinematic);
        drone.SendCommand(FlightCommand.GoTo(new Vector3(100f, 10f, 0f)));

        drone.Step(1.0, Vector3.Zero);

        drone.FlightModel.State.Position.X.Should().BeGreaterThan(start.X);
    }

    // 4. Step with non-zero wind affects position even when hovering.
    [Fact]
    public void Step_WithWeatherWind_AffectsPosition()
    {
        var start = new Vector3(0f, 50f, 0f);
        var wind = new Vector3(10f, 0f, 0f);
        var drone = new SimulatedDrone("d2", start, FlightModelType.Kinematic);
        drone.SendCommand(FlightCommand.Hover());

        drone.Step(1.0, wind);

        drone.FlightModel.State.Position.X.Should().BeApproximately(wind.X * 1f, 0.01f);
    }

    // 5. Detection probability is higher in clear conditions than in foggy conditions.
    [Fact]
    public void DetectionProbability_AffectedByVisibility()
    {
        var drone = new SimulatedDrone("d3", Vector3.Zero, FlightModelType.Kinematic);

        var clear = drone.GetDetectionProbability(1.0);
        var foggy = drone.GetDetectionProbability(0.1);

        clear.Should().BeGreaterThan(foggy);
    }

    // 6. Kinematic model type produces a KinematicFlightModel instance.
    [Fact]
    public void Kinematic_ModelIsKinematic()
    {
        var drone = new SimulatedDrone("k1", Vector3.Zero, FlightModelType.Kinematic);
        drone.FlightModel.Should().BeOfType<KinematicFlightModel>();
    }

    // 7. Quadrotor model type produces a QuadrotorFlightModel instance.
    [Fact]
    public void Quadrotor_ModelIsQuadrotor()
    {
        var drone = new SimulatedDrone("q1", Vector3.Zero, FlightModelType.Quadrotor);
        drone.FlightModel.Should().BeOfType<QuadrotorFlightModel>();
    }
}
