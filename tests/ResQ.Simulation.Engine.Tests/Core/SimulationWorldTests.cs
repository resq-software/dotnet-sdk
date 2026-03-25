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
using NSubstitute;
using ResQ.Simulation.Engine.Core;
using ResQ.Simulation.Engine.Environment;
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Core;

public class SimulationWorldTests
{
    private static SimulationWorld CreateWorld(SimulationConfig? config = null)
    {
        config ??= new SimulationConfig { ClockMode = ClockMode.Stepped, Seed = 42 };
        var terrain = Substitute.For<ITerrain>();
        terrain.GetElevation(Arg.Any<double>(), Arg.Any<double>()).Returns(0.0);
        var weather = Substitute.For<IWeatherSystem>();
        weather.GetWind(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>()).Returns(Vector3.Zero);
        weather.Visibility.Returns(1.0);
        return new SimulationWorld(config, terrain, weather);
    }

    // 1. Freshly constructed world has empty drone and structure collections and zero elapsed time.
    [Fact]
    public void Constructor_InitializesEmptyWorld()
    {
        var world = CreateWorld();

        world.Drones.Should().BeEmpty();
        world.Structures.Should().BeEmpty();
        world.Clock.ElapsedTime.Should().Be(0.0);
    }

    // 2. AddDrone adds the drone to Drones and returns the correct instance.
    [Fact]
    public void AddDrone_AddsToCollection()
    {
        var world = CreateWorld();
        var drone = world.AddDrone("alpha", new Vector3(0f, 10f, 0f));

        world.Drones.Should().ContainSingle();
        world.Drones[0].Should().BeSameAs(drone);
        world.Drones[0].Id.Should().Be("alpha");
    }

    // 3. Adding a second drone with the same id throws ArgumentException.
    [Fact]
    public void AddDrone_DuplicateId_Throws()
    {
        var world = CreateWorld();
        world.AddDrone("dup", Vector3.Zero);

        var act = () => world.AddDrone("dup", new Vector3(1f, 0f, 0f));
        act.Should().Throw<ArgumentException>();
    }

    // 4. Step advances the clock and moves a drone that has been given a GoTo command.
    [Fact]
    public void Step_AdvancesClockAndDrones()
    {
        var world = CreateWorld();
        var drone = world.AddDrone("d1", new Vector3(0f, 10f, 0f));
        drone.SendCommand(FlightCommand.GoTo(new Vector3(100f, 10f, 0f)));

        world.Step();

        world.Clock.ElapsedTime.Should().BeGreaterThan(0.0);
        drone.FlightModel.State.Position.X.Should().BeGreaterThan(0f);
    }

    // 5. Two worlds with the same seed produce identical drone positions after 60 steps.
    [Fact]
    public void Step_MultipleSteps_Deterministic()
    {
        var config = new SimulationConfig { ClockMode = ClockMode.Stepped, Seed = 99 };

        var world1 = CreateWorld(config);
        var world2 = CreateWorld(config);

        var drone1 = world1.AddDrone("d", new Vector3(0f, 10f, 0f));
        var drone2 = world2.AddDrone("d", new Vector3(0f, 10f, 0f));

        drone1.SendCommand(FlightCommand.GoTo(new Vector3(200f, 10f, 0f)));
        drone2.SendCommand(FlightCommand.GoTo(new Vector3(200f, 10f, 0f)));

        for (var i = 0; i < 60; i++)
        {
            world1.Step();
            world2.Step();
        }

        drone1.FlightModel.State.Position.X
            .Should().BeApproximately(drone2.FlightModel.State.Position.X, 1e-4f);
    }

    // 6. AddStructure adds the structure to the Structures collection.
    [Fact]
    public void AddStructure_AddsToCollection()
    {
        var world = CreateWorld();
        var structure = world.AddStructure("bldg-1", new Vector3(50f, 0f, 50f), new Vector3(5f, 10f, 5f));

        world.Structures.Should().ContainSingle();
        world.Structures[0].Should().BeSameAs(structure);
        world.Structures[0].Id.Should().Be("bldg-1");
    }

    // 7. When the clock is paused, Step does not advance elapsed time.
    [Fact]
    public void Step_WhenPaused_DoesNotAdvance()
    {
        var world = CreateWorld();
        world.AddDrone("d1", new Vector3(0f, 10f, 0f));
        world.Clock.Pause();

        world.Step();

        world.Clock.ElapsedTime.Should().Be(0.0);
    }
}
