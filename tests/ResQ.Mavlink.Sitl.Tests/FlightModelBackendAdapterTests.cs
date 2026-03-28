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
using ResQ.Mavlink.Sitl;
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Mavlink.Sitl.Tests;

public sealed class FlightModelBackendAdapterTests
{
    private static IFlightModel MakeModel(DronePhysicsState state)
    {
        var model = Substitute.For<IFlightModel>();
        model.State.Returns(state);
        return model;
    }

    [Fact]
    public void Capabilities_DoesNotIncludeGps()
    {
        var model = MakeModel(DronePhysicsState.AtPosition(Vector3.Zero));
        var adapter = new FlightModelBackendAdapter(model);

        adapter.Capabilities.HasFlag(FlightBackendCapabilities.Gps).Should().BeFalse();
    }

    [Fact]
    public void Capabilities_IsNone()
    {
        var model = MakeModel(DronePhysicsState.AtPosition(Vector3.Zero));
        var adapter = new FlightModelBackendAdapter(model);

        adapter.Capabilities.Should().Be(FlightBackendCapabilities.None);
    }

    [Fact]
    public async Task InitializeAsync_AcceptsDroneConfigWithoutThrowing()
    {
        var model = MakeModel(DronePhysicsState.AtPosition(Vector3.Zero));
        var adapter = new FlightModelBackendAdapter(model);
        var config = new DroneConfig("drone-1", new Vector3(1, 2, 3));

        var act = async () => await adapter.InitializeAsync(config);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StepAsync_DelegatesToModelStep_WithWind()
    {
        var expectedState = new DronePhysicsState(
            new Vector3(10, 5, 0), new Vector3(1, 0, 0), Quaternion.Identity, Vector3.Zero, 95.0);
        var model = MakeModel(expectedState);
        var adapter = new FlightModelBackendAdapter(model);

        var wind = new Vector3(2, 0, 1);
        var result = await adapter.StepAsync(0.01, wind);

        model.Received(1).Step(0.01, wind);
        result.Should().Be(expectedState);
    }

    [Fact]
    public async Task SendCommandAsync_DelegatesToModelApplyCommand()
    {
        var model = MakeModel(DronePhysicsState.AtPosition(Vector3.Zero));
        var adapter = new FlightModelBackendAdapter(model);

        var cmd = FlightCommand.Hover();
        await adapter.SendCommandAsync(cmd);

        model.Received(1).ApplyCommand(cmd);
    }

    [Fact]
    public async Task DisposeAsync_DoesNotThrow()
    {
        var model = MakeModel(DronePhysicsState.AtPosition(Vector3.Zero));
        var adapter = new FlightModelBackendAdapter(model);

        var act = async () => await adapter.DisposeAsync();

        await act.Should().NotThrowAsync();
    }
}
