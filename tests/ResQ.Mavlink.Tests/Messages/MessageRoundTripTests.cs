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
using ResQ.Mavlink.Enums;
using ResQ.Mavlink.Messages;
using Xunit;

namespace ResQ.Mavlink.Tests.Messages;

public class MessageRoundTripTests
{
    [Fact]
    public void CommandLong_RoundTrips()
    {
        var original = new CommandLong
        {
            TargetSystem = 1,
            TargetComponent = 1,
            Command = MavCmd.ComponentArmDisarm,
            Confirmation = 0,
            Param1 = 1.0f, // arm
        };

        Span<byte> buf = stackalloc byte[CommandLong.PayloadSize];
        original.Serialize(buf);
        var parsed = CommandLong.Deserialize(buf);

        parsed.Command.Should().Be(MavCmd.ComponentArmDisarm);
        parsed.Param1.Should().Be(1.0f);
        parsed.TargetSystem.Should().Be(1);
    }

    [Fact]
    public void CommandAck_RoundTrips()
    {
        var original = new CommandAck
        {
            Command = MavCmd.ComponentArmDisarm,
            Result = MavResult.Accepted,
        };

        Span<byte> buf = stackalloc byte[CommandAck.PayloadSize];
        original.Serialize(buf);
        var parsed = CommandAck.Deserialize(buf);

        parsed.Command.Should().Be(MavCmd.ComponentArmDisarm);
        parsed.Result.Should().Be(MavResult.Accepted);
    }

    [Fact]
    public void StatusText_RoundTrips()
    {
        var original = new StatusText
        {
            Severity = MavSeverity.Info,
            Text = "PreArm: Ready to arm",
        };

        Span<byte> buf = stackalloc byte[StatusText.PayloadSize];
        original.Serialize(buf);
        var parsed = StatusText.Deserialize(buf);

        parsed.Severity.Should().Be(MavSeverity.Info);
        parsed.Text.Should().StartWith("PreArm: Ready to arm");
    }

    [Fact]
    public void ParamValue_RoundTrips()
    {
        var original = new ParamValue
        {
            ParamId = "ARMING_CHECK",
            ParamValue_ = 1.0f,
            ParamType = 9, // REAL32
            ParamCount = 500,
            ParamIndex = 42,
        };

        Span<byte> buf = stackalloc byte[ParamValue.PayloadSize];
        original.Serialize(buf);
        var parsed = ParamValue.Deserialize(buf);

        parsed.ParamId.Should().Be("ARMING_CHECK");
        parsed.ParamValue_.Should().Be(1.0f);
        parsed.ParamIndex.Should().Be(42);
    }

    [Fact]
    public void Attitude_RoundTrips()
    {
        var original = new Attitude
        {
            TimeBootMs = 100,
            Roll = 0.1f,
            Pitch = -0.05f,
            Yaw = 1.57f,
            Rollspeed = 0.01f,
            Pitchspeed = 0.0f,
            Yawspeed = 0.02f,
        };

        Span<byte> buf = stackalloc byte[Attitude.PayloadSize];
        original.Serialize(buf);
        var parsed = Attitude.Deserialize(buf);

        parsed.Roll.Should().BeApproximately(0.1f, 1e-6f);
        parsed.Yaw.Should().BeApproximately(1.57f, 1e-6f);
    }

    [Fact]
    public void SysStatus_RoundTrips()
    {
        var original = new SysStatus
        {
            OnboardControlSensorsPresent = 0xFFFF,
            OnboardControlSensorsEnabled = 0x0F0F,
            OnboardControlSensorsHealth = 0xF0F0,
            Load = 800,
            VoltageBattery = 12600,
            CurrentBattery = 500,
            BatteryRemaining = 80,
        };

        Span<byte> buf = stackalloc byte[SysStatus.PayloadSize];
        original.Serialize(buf);
        var parsed = SysStatus.Deserialize(buf);

        parsed.VoltageBattery.Should().Be(12600);
        parsed.BatteryRemaining.Should().Be(80);
    }

    [Fact]
    public void GpsRawInt_RoundTrips()
    {
        var original = new GpsRawInt
        {
            TimeUsec = 123456789,
            Lat = 473977418,
            Lon = 85255792,
            Alt = 408000,
            FixType = GpsFixType.Fix3d,
            SatellitesVisible = 12,
            Vel = 250,
        };

        Span<byte> buf = stackalloc byte[GpsRawInt.PayloadSize];
        original.Serialize(buf);
        var parsed = GpsRawInt.Deserialize(buf);

        parsed.Lat.Should().Be(473977418);
        parsed.FixType.Should().Be(GpsFixType.Fix3d);
        parsed.SatellitesVisible.Should().Be(12);
    }

    [Fact]
    public void MissionItemInt_RoundTrips()
    {
        var original = new MissionItemInt
        {
            Seq = 3,
            Command = MavCmd.NavWaypoint,
            Frame = MavFrame.GlobalRelativeAlt,
            X = 473977418,
            Y = 85255792,
            Z = 50.0f,
            Current = 0,
            Autocontinue = 1,
        };

        Span<byte> buf = stackalloc byte[MissionItemInt.PayloadSize];
        original.Serialize(buf);
        var parsed = MissionItemInt.Deserialize(buf);

        parsed.Seq.Should().Be(3);
        parsed.Command.Should().Be(MavCmd.NavWaypoint);
        parsed.Frame.Should().Be(MavFrame.GlobalRelativeAlt);
    }

    [Fact]
    public void ExtendedSysState_RoundTrips()
    {
        var original = new ExtendedSysState { VtolState = 1, LandedState = 2 };
        Span<byte> buf = stackalloc byte[ExtendedSysState.PayloadSize];
        original.Serialize(buf);
        var parsed = ExtendedSysState.Deserialize(buf);

        parsed.VtolState.Should().Be(1);
        parsed.LandedState.Should().Be(2);
    }

    [Fact]
    public void HomePosition_RoundTrips()
    {
        var original = new HomePosition
        {
            Latitude = 473977418,
            Longitude = 85255792,
            Altitude = 408000,
            X = 0f,
            Y = 0f,
            Z = 0f,
            Q1 = 1f,
            Q2 = 0f,
            Q3 = 0f,
            Q4 = 0f,
            ApproachX = 0f,
            ApproachY = 0f,
            ApproachZ = -1f,
        };

        Span<byte> buf = stackalloc byte[HomePosition.PayloadSize];
        original.Serialize(buf);
        var parsed = HomePosition.Deserialize(buf);

        parsed.Latitude.Should().Be(473977418);
        parsed.Altitude.Should().Be(408000);
        parsed.Q1.Should().Be(1f);
    }
}
