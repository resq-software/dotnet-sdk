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
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Sitl;
using Xunit;

namespace ResQ.Mavlink.Sitl.Tests;

public sealed class SitlTelemetryMapperTests
{
    private static GlobalPositionInt MakePosition(
        int latDegE7 = -353_632_620,
        int lonDegE7 = 1_491_652_370,
        int relAltMm = 10_000,
        short vxCmS = 0,
        short vyCmS = 0,
        short vzCmS = 0)
        => new()
        {
            Lat = latDegE7,
            Lon = lonDegE7,
            RelativeAlt = relAltMm,
            Vx = vxCmS,
            Vy = vyCmS,
            Vz = vzCmS,
        };

    private static Attitude MakeAttitude(
        float roll = 0f,
        float pitch = 0f,
        float yaw = 0f,
        float rollSpeed = 0f,
        float pitchSpeed = 0f,
        float yawSpeed = 0f)
        => new()
        {
            Roll = roll,
            Pitch = pitch,
            Yaw = yaw,
            Rollspeed = rollSpeed,
            Pitchspeed = pitchSpeed,
            Yawspeed = yawSpeed,
        };

    [Fact]
    public void Map_PositionConversion_ProducesNonZeroXAndZ()
    {
        var pos = MakePosition();
        var att = MakeAttitude();

        var state = SitlTelemetryMapper.Map(pos, att);

        // Non-zero lat and lon should produce non-zero X (East) and Z (South) components.
        state.Position.X.Should().NotBe(0f, "non-zero longitude should produce non-zero East offset");
        state.Position.Z.Should().NotBe(0f, "non-zero latitude should produce non-zero South offset");
    }

    [Fact]
    public void Map_RelativeAltitude_MapsToPositionY()
    {
        // 10 000 mm = 10 m
        var pos = MakePosition(relAltMm: 10_000);
        var att = MakeAttitude();

        var state = SitlTelemetryMapper.Map(pos, att);

        state.Position.Y.Should().BeApproximately(10.0f, 0.001f);
    }

    [Fact]
    public void Map_ZeroAttitude_ProducesNearIdentityOrientation()
    {
        var pos = MakePosition();
        var att = MakeAttitude(roll: 0, pitch: 0, yaw: 0);

        var state = SitlTelemetryMapper.Map(pos, att);

        // Identity quaternion is (X=0, Y=0, Z=0, W=1).
        state.Orientation.W.Should().BeApproximately(1.0f, 0.001f);
        state.Orientation.X.Should().BeApproximately(0.0f, 0.001f);
        state.Orientation.Y.Should().BeApproximately(0.0f, 0.001f);
        state.Orientation.Z.Should().BeApproximately(0.0f, 0.001f);
    }

    [Fact]
    public void Map_NonZeroAttitude_SetsNonIdentityOrientation()
    {
        var pos = MakePosition();
        var att = MakeAttitude(roll: 0.5f, pitch: 0.3f, yaw: 1.2f);

        var state = SitlTelemetryMapper.Map(pos, att);

        // Non-zero angles should produce a non-identity quaternion.
        var isIdentity = Math.Abs(state.Orientation.W - 1.0f) < 0.001f
            && Math.Abs(state.Orientation.X) < 0.001f
            && Math.Abs(state.Orientation.Y) < 0.001f
            && Math.Abs(state.Orientation.Z) < 0.001f;

        isIdentity.Should().BeFalse("non-zero Euler angles should produce a non-identity quaternion");
    }

    [Fact]
    public void Map_VelocityConversion_NED_To_ENU()
    {
        // Vx = North 100 cm/s, Vy = East 200 cm/s, Vz = Down 50 cm/s
        var pos = MakePosition(vxCmS: 100, vyCmS: 200, vzCmS: 50);
        var att = MakeAttitude();

        var state = SitlTelemetryMapper.Map(pos, att);

        // ENU: X = East = Vy/100 = 2.0, Y = Up = -Vz/100 = -0.5, Z = South = -Vx/100 = -1.0
        state.Velocity.X.Should().BeApproximately(2.0f, 0.001f, "East = Vy");
        state.Velocity.Y.Should().BeApproximately(-0.5f, 0.001f, "Up = -Vz");
        state.Velocity.Z.Should().BeApproximately(-1.0f, 0.001f, "South = -Vx");
    }

    [Fact]
    public void Map_DefaultBatteryPercent_Is100()
    {
        var pos = MakePosition();
        var att = MakeAttitude();

        var state = SitlTelemetryMapper.Map(pos, att);

        state.BatteryPercent.Should().Be(100.0);
    }

    [Fact]
    public void Map_CustomBatteryPercent_IsPassedThrough()
    {
        var pos = MakePosition();
        var att = MakeAttitude();

        var state = SitlTelemetryMapper.Map(pos, att, batteryPercent: 42.5);

        state.BatteryPercent.Should().Be(42.5);
    }
}
