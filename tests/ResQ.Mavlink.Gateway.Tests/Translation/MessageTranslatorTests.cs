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
using ResQ.Core;
using ResQ.Mavlink.Enums;
using ResQ.Mavlink.Gateway.Translation;
using ResQ.Mavlink.Messages;
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Mavlink.Gateway.Tests.Translation;

public class MessageTranslatorTests
{
    // ── MapToTelemetry ───────────────────────────────────────────────────────

    private static GlobalPositionInt MakePos(int latE7, int lonE7, int altMm, short vx = 0, short vy = 0, short vz = 0)
        => new() { Lat = latE7, Lon = lonE7, Alt = altMm, Vx = vx, Vy = vy, Vz = vz };

    [Fact]
    public void MapToTelemetry_ConvertsLatLonDegE7ToDecimalDegrees()
    {
        // 37.7749° N, -122.4194° W expressed as degE7
        var pos = MakePos(377_749_000, -1_224_194_000, 0);

        var packet = MessageTranslator.MapToTelemetry("drn-001", pos, null, null, null);

        packet.Position.Latitude.Should().BeApproximately(37.7749, 1e-4);
        packet.Position.Longitude.Should().BeApproximately(-122.4194, 1e-4);
    }

    [Fact]
    public void MapToTelemetry_RoundTrip_DegE7PreservesPrecision()
    {
        // Encode a known coordinate, translate, re-encode, compare
        const double srcLat = 51.5074;
        const double srcLon = -0.1278;
        int latE7 = (int)Math.Round(srcLat * 1e7);
        int lonE7 = (int)Math.Round(srcLon * 1e7);

        var pos = MakePos(latE7, lonE7, 0);
        var packet = MessageTranslator.MapToTelemetry("drn-002", pos, null, null, null);

        // Re-encode back to degE7
        int roundTrippedLatE7 = (int)Math.Round(packet.Position.Latitude * 1e7);
        int roundTrippedLonE7 = (int)Math.Round(packet.Position.Longitude * 1e7);

        roundTrippedLatE7.Should().Be(latE7);
        roundTrippedLonE7.Should().Be(lonE7);
    }

    [Fact]
    public void MapToTelemetry_ConvertsMmToMetres()
    {
        var pos = MakePos(0, 0, 120_000); // 120 m in mm
        var packet = MessageTranslator.MapToTelemetry("drn-003", pos, null, null, null);

        packet.Position.Altitude.Should().BeApproximately(120.0, 1e-9);
    }

    [Fact]
    public void MapToTelemetry_ZeroLatLon_MapsCorrectly()
    {
        var pos = MakePos(0, 0, 0);
        var packet = MessageTranslator.MapToTelemetry("drn-004", pos, null, null, null);

        packet.Position.Latitude.Should().Be(0.0);
        packet.Position.Longitude.Should().Be(0.0);
        packet.Position.Altitude.Should().Be(0.0);
    }

    [Fact]
    public void MapToTelemetry_BatteryRemaining_NegativeOne_MapsToZero()
    {
        var pos = MakePos(0, 0, 0);
        var sys = new SysStatus { BatteryRemaining = -1, VoltageBattery = 0 };

        var packet = MessageTranslator.MapToTelemetry("drn-005", pos, null, sys, null);

        packet.BatteryPercent.Should().Be(0f);
    }

    [Fact]
    public void MapToTelemetry_BatteryRemaining_PositiveValue_MapsDirectly()
    {
        var pos = MakePos(0, 0, 0);
        var sys = new SysStatus { BatteryRemaining = 75, VoltageBattery = 12_600 };

        var packet = MessageTranslator.MapToTelemetry("drn-006", pos, null, sys, null);

        packet.BatteryPercent.Should().Be(75f);
        packet.BatteryVoltage.Should().BeApproximately(12.6f, 1e-3f);
    }

    [Fact]
    public void MapToTelemetry_NoSysStatus_BatteryIsZero()
    {
        var pos = MakePos(0, 0, 0);
        var packet = MessageTranslator.MapToTelemetry("drn-007", pos, null, null, null);

        packet.BatteryPercent.Should().Be(0f);
        packet.BatteryVoltage.Should().Be(0f);
    }

    [Fact]
    public void MapToTelemetry_Heartbeat_ActiveState_MapsToInFlight()
    {
        var pos = MakePos(0, 0, 0);
        var hb = new Heartbeat { SystemStatus = MavState.Active };

        var packet = MessageTranslator.MapToTelemetry("drn-008", pos, null, null, hb);

        packet.Status.Should().Be(DroneStatus.InFlight);
    }

    [Fact]
    public void MapToTelemetry_Heartbeat_EmergencyState_MapsToEmergency()
    {
        var pos = MakePos(0, 0, 0);
        var hb = new Heartbeat { SystemStatus = MavState.Emergency };

        var packet = MessageTranslator.MapToTelemetry("drn-009", pos, null, null, hb);

        packet.Status.Should().Be(DroneStatus.Emergency);
    }

    [Fact]
    public void MapToTelemetry_NoHeartbeat_StatusIsIdle()
    {
        var pos = MakePos(0, 0, 0);
        var packet = MessageTranslator.MapToTelemetry("drn-010", pos, null, null, null);

        packet.Status.Should().Be(DroneStatus.Idle);
    }

    [Fact]
    public void MapToTelemetry_VelocityConvertedFromCmToMs()
    {
        var pos = MakePos(0, 0, 0, vx: 500, vy: -300, vz: 100); // 5 m/s N, -3 m/s E, 1 m/s D

        var packet = MessageTranslator.MapToTelemetry("drn-011", pos, null, null, null);

        packet.Velocity!.Vx.Should().BeApproximately(5.0, 1e-9);
        packet.Velocity.Vy.Should().BeApproximately(-3.0, 1e-9);
        packet.Velocity.Vz.Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void MapToTelemetry_DroneIdPreserved()
    {
        var pos = MakePos(0, 0, 0);
        var packet = MessageTranslator.MapToTelemetry("drn-xyz-999", pos, null, null, null);

        packet.DroneId.Should().Be("drn-xyz-999");
    }

    // ── MapToSetPositionTarget ───────────────────────────────────────────────

    [Fact]
    public void MapToSetPositionTarget_ConvertsDecimalDegToE7()
    {
        var msg = MessageTranslator.MapToSetPositionTarget(37.7749, -122.4194, 100f);

        msg.LatInt.Should().Be((int)Math.Round(37.7749 * 1e7));
        msg.LonInt.Should().Be((int)Math.Round(-122.4194 * 1e7));
        msg.Alt.Should().BeApproximately(100f, 1e-3f);
    }

    [Fact]
    public void MapToSetPositionTarget_ZeroPosition_Correct()
    {
        var msg = MessageTranslator.MapToSetPositionTarget(0.0, 0.0, 0f);

        msg.LatInt.Should().Be(0);
        msg.LonInt.Should().Be(0);
        msg.Alt.Should().Be(0f);
    }

    // ── MapFlightCommandToMavlink ────────────────────────────────────────────

    [Theory]
    [InlineData(FlightCommandType.Hover, MavCmd.NavLoiterUnlim)]
    [InlineData(FlightCommandType.GoToWaypoint, MavCmd.NavWaypoint)]
    [InlineData(FlightCommandType.ReturnToLaunch, MavCmd.NavReturnToLaunch)]
    [InlineData(FlightCommandType.Land, MavCmd.NavLand)]
    public void MapFlightCommandToMavlink_MapsAllCommandTypes(FlightCommandType input, MavCmd expected)
    {
        var cmd = MessageTranslator.MapFlightCommandToMavlink(input);

        cmd.Command.Should().Be(expected);
    }

    [Fact]
    public void MapFlightCommandToMavlink_InvalidType_Throws()
    {
        var act = () => MessageTranslator.MapFlightCommandToMavlink((FlightCommandType)999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
