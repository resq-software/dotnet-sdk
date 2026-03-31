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
using ResQ.Mavlink.Gateway.State;
using ResQ.Mavlink.Messages;
using Xunit;

namespace ResQ.Mavlink.Gateway.Tests.State;

public class VehicleStateTrackerTests
{
    private static readonly byte SysId = 1;

    // ── GlobalPositionInt ────────────────────────────────────────────────────

    [Fact]
    public void Update_GlobalPositionInt_UpdatesPositionFields()
    {
        var tracker = new VehicleStateTracker();
        var msg = new GlobalPositionInt
        {
            Lat = 377_749_000,      // 37.7749°
            Lon = -1_224_194_000,   // -122.4194°
            Alt = 100_000,          // 100 m
            RelativeAlt = 50_000,   // 50 m
        };

        tracker.Update(SysId, msg);

        var state = tracker.GetVehicle(SysId)!;
        state.Latitude.Should().BeApproximately(37.7749, 1e-4);
        state.Longitude.Should().BeApproximately(-122.4194, 1e-4);
        state.AltitudeMetres.Should().BeApproximately(100.0, 1e-3);
        state.RelativeAltMetres.Should().BeApproximately(50.0, 1e-3);
    }

    // ── Attitude ─────────────────────────────────────────────────────────────

    [Fact]
    public void Update_Attitude_UpdatesAttitudeFields()
    {
        var tracker = new VehicleStateTracker();
        var msg = new Attitude { Roll = 0.1f, Pitch = 0.2f, Yaw = 1.5f };

        tracker.Update(SysId, msg);

        var state = tracker.GetVehicle(SysId)!;
        state.Roll.Should().BeApproximately(0.1f, 1e-5f);
        state.Pitch.Should().BeApproximately(0.2f, 1e-5f);
        state.Yaw.Should().BeApproximately(1.5f, 1e-5f);
    }

    // ── SysStatus ────────────────────────────────────────────────────────────

    [Fact]
    public void Update_SysStatus_UpdatesBatteryFields()
    {
        var tracker = new VehicleStateTracker();
        var msg = new SysStatus
        {
            BatteryRemaining = 75,
            VoltageBattery = 12_600, // 12.6 V in millivolts
        };

        tracker.Update(SysId, msg);

        var state = tracker.GetVehicle(SysId)!;
        state.BatteryPercent.Should().BeApproximately(75.0, 1e-3);
        state.BatteryVoltage.Should().BeApproximately(12.6, 1e-3);
    }

    [Fact]
    public void Update_SysStatus_NegativeBatteryRemaining_YieldsZeroPercent()
    {
        var tracker = new VehicleStateTracker();
        var msg = new SysStatus { BatteryRemaining = -1, VoltageBattery = 0 };

        tracker.Update(SysId, msg);

        tracker.GetVehicle(SysId)!.BatteryPercent.Should().Be(0.0);
    }

    // ── Heartbeat ────────────────────────────────────────────────────────────

    [Fact]
    public void Update_Heartbeat_UpdatesStatusAndArmedFlag()
    {
        var tracker = new VehicleStateTracker();
        var msg = new Heartbeat
        {
            SystemStatus = MavState.Active,
            BaseMode = MavModeFlag.SafetyArmed,
        };

        tracker.Update(SysId, msg);

        var state = tracker.GetVehicle(SysId)!;
        state.Status.Should().Be("INFLIGHT");
        state.IsArmed.Should().BeTrue();
    }

    [Fact]
    public void Update_Heartbeat_Disarmed_SetsIsArmedFalse()
    {
        var tracker = new VehicleStateTracker();
        var msg = new Heartbeat
        {
            SystemStatus = MavState.Standby,
            BaseMode = (MavModeFlag)0,
        };

        tracker.Update(SysId, msg);

        var state = tracker.GetVehicle(SysId)!;
        state.IsArmed.Should().BeFalse();
        state.Status.Should().Be("IDLE");
    }

    // ── MissionCurrent ───────────────────────────────────────────────────────

    [Fact]
    public void Update_MissionCurrent_UpdatesCurrentWaypoint()
    {
        var tracker = new VehicleStateTracker();
        tracker.Update(SysId, new MissionCurrent { Seq = 7 });

        tracker.GetVehicle(SysId)!.CurrentWaypoint.Should().Be(7);
    }

    // ── LastSeen ─────────────────────────────────────────────────────────────

    [Fact]
    public void Update_SetsLastSeenToApproximatelyNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var tracker = new VehicleStateTracker();
        tracker.Update(SysId, new Attitude());

        tracker.GetVehicle(SysId)!.LastSeen.Should().BeAfter(before);
    }

    // ── Multi-vehicle tracking ────────────────────────────────────────────────

    [Fact]
    public void MultipleVehicles_TrackedIndependentlyBySystemId()
    {
        var tracker = new VehicleStateTracker();
        tracker.Update(1, new Attitude { Roll = 0.1f });
        tracker.Update(2, new Attitude { Roll = 0.9f });

        tracker.GetVehicle(1)!.Roll.Should().BeApproximately(0.1f, 1e-5f);
        tracker.GetVehicle(2)!.Roll.Should().BeApproximately(0.9f, 1e-5f);
    }

    // ── GetVehicle ───────────────────────────────────────────────────────────

    [Fact]
    public void GetVehicle_UnknownSystemId_ReturnsNull()
    {
        var tracker = new VehicleStateTracker();

        tracker.GetVehicle(99).Should().BeNull();
    }

    // ── GetAllVehicles ───────────────────────────────────────────────────────

    [Fact]
    public void GetAllVehicles_ReturnsAllTracked()
    {
        var tracker = new VehicleStateTracker();
        tracker.Update(1, new Heartbeat());
        tracker.Update(2, new Heartbeat());
        tracker.Update(3, new Heartbeat());

        tracker.GetAllVehicles().Should().HaveCount(3);
    }

    // ── ToTelemetryPacket ─────────────────────────────────────────────────────

    [Fact]
    public void ToTelemetryPacket_UnknownSystemId_ReturnsNull()
    {
        var tracker = new VehicleStateTracker();

        tracker.ToTelemetryPacket(99).Should().BeNull();
    }

    [Fact]
    public void ToTelemetryPacket_ProducesValidPacket()
    {
        var tracker = new VehicleStateTracker();
        tracker.Update(SysId, new GlobalPositionInt
        {
            Lat = 517_400_000,    // 51.74°
            Lon = -10_000_000,   // -1.0°
            Alt = 200_000,       // 200 m
            RelativeAlt = 80_000,
        });
        tracker.Update(SysId, new SysStatus { BatteryRemaining = 80, VoltageBattery = 16_800 });
        tracker.Update(SysId, new Heartbeat
        {
            SystemStatus = MavState.Active,
            BaseMode = MavModeFlag.SafetyArmed,
        });

        var packet = tracker.ToTelemetryPacket(SysId)!;

        packet.DroneId.Should().Be($"mavlink-{SysId}");
        packet.Position.Latitude.Should().BeApproximately(51.74, 1e-4);
        packet.Position.Longitude.Should().BeApproximately(-1.0, 1e-4);
        packet.Position.Altitude.Should().BeApproximately(200.0, 1e-3);
        packet.BatteryPercent.Should().BeApproximately(80f, 1e-3f);
        packet.BatteryVoltage.Should().BeApproximately(16.8f, 1e-3f);
        packet.Status.Should().Be(DroneStatus.InFlight);
    }
}
