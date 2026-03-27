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
using System.Text.Json;
using FluentAssertions;
using ResQ.Mavlink.Sitl;
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Mavlink.Sitl.Tests;

public sealed class JsonPhysicsBridgeTests
{
    private static readonly DronePhysicsState AtRestState =
        DronePhysicsState.AtPosition(Vector3.Zero);

    [Fact]
    public void BuildSensorJson_ProducesValidJson()
    {
        var json = JsonPhysicsBridge.BuildSensorJson(AtRestState, timestampMicros: 1_000_000L);

        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow("BuildSensorJson must produce well-formed JSON");
    }

    [Fact]
    public void BuildSensorJson_ContainsTimestamp()
    {
        var json = JsonPhysicsBridge.BuildSensorJson(AtRestState, timestampMicros: 12345L);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("timestamp", out var ts).Should().BeTrue();
        ts.GetInt64().Should().Be(12345L);
    }

    [Fact]
    public void BuildSensorJson_ContainsImuGyroArray()
    {
        var json = JsonPhysicsBridge.BuildSensorJson(AtRestState, timestampMicros: 0L);

        using var doc = JsonDocument.Parse(json);
        var imu = doc.RootElement.GetProperty("imu");
        imu.TryGetProperty("gyro", out var gyro).Should().BeTrue();
        gyro.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public void BuildSensorJson_ContainsImuAccelBodyArray()
    {
        var json = JsonPhysicsBridge.BuildSensorJson(AtRestState, timestampMicros: 0L);

        using var doc = JsonDocument.Parse(json);
        var imu = doc.RootElement.GetProperty("imu");
        imu.TryGetProperty("accel_body", out var accel).Should().BeTrue();
        accel.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public void BuildSensorJson_AtRest_AccelBodyZ_IsApproximatelyGravity()
    {
        var json = JsonPhysicsBridge.BuildSensorJson(AtRestState, timestampMicros: 0L);

        using var doc = JsonDocument.Parse(json);
        var accel = doc.RootElement.GetProperty("imu").GetProperty("accel_body");
        var accelZ = accel[2].GetDouble();

        // At rest, body Z-down accel should be approximately +9.81 m/s² (gravity).
        accelZ.Should().BeApproximately(9.80665, 0.01,
            "at rest, accel_body[Z] should read approximately +9.81 m/s² (gravity)");
    }

    [Fact]
    public void BuildSensorJson_ContainsPositionArray()
    {
        var state = new DronePhysicsState(
            new Vector3(10, 20, 30), Vector3.Zero, Quaternion.Identity, Vector3.Zero, 100.0);

        var json = JsonPhysicsBridge.BuildSensorJson(state, timestampMicros: 0L);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("position", out var pos).Should().BeTrue();
        pos.GetArrayLength().Should().Be(3);
        pos[0].GetDouble().Should().BeApproximately(10.0, 0.001);
    }

    [Fact]
    public void BuildSensorJson_ContainsQuaternionArray()
    {
        var json = JsonPhysicsBridge.BuildSensorJson(AtRestState, timestampMicros: 0L);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("quaternion", out var q).Should().BeTrue();
        q.GetArrayLength().Should().Be(4);
    }

    [Fact]
    public void BuildSensorJson_ContainsAirspeed()
    {
        var json = JsonPhysicsBridge.BuildSensorJson(AtRestState, timestampMicros: 0L);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("airspeed", out var airspeed).Should().BeTrue();
        airspeed.GetDouble().Should().Be(0.0, "drone at rest has zero airspeed");
    }

    [Fact]
    public void BuildSensorJson_MovingDrone_AirspeedIsNonZero()
    {
        var state = new DronePhysicsState(
            Vector3.Zero, new Vector3(3, 4, 0), Quaternion.Identity, Vector3.Zero, 100.0);

        var json = JsonPhysicsBridge.BuildSensorJson(state, timestampMicros: 0L);

        using var doc = JsonDocument.Parse(json);
        var airspeed = doc.RootElement.GetProperty("airspeed").GetDouble();
        // |v| = sqrt(9+16+0) = 5 m/s
        airspeed.Should().BeApproximately(5.0, 0.001);
    }
}
