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
using Xunit;

namespace ResQ.Mavlink.Gateway.Tests.Translation;

public class MavStateMapperTests
{
    // ── MapDroneStatus ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(MavState.Uninit)]
    [InlineData(MavState.Boot)]
    [InlineData(MavState.Calibrating)]
    [InlineData(MavState.Standby)]
    public void MapDroneStatus_IdleStates_ReturnsIdle(MavState state)
    {
        MavStateMapper.MapDroneStatus(state).Should().Be(DroneStatus.Idle);
    }

    [Fact]
    public void MapDroneStatus_Active_ReturnsInFlight()
    {
        MavStateMapper.MapDroneStatus(MavState.Active).Should().Be(DroneStatus.InFlight);
    }

    [Theory]
    [InlineData(MavState.Critical)]
    [InlineData(MavState.Emergency)]
    public void MapDroneStatus_CriticalOrEmergency_ReturnsEmergency(MavState state)
    {
        MavStateMapper.MapDroneStatus(state).Should().Be(DroneStatus.Emergency);
    }

    [Theory]
    [InlineData(MavState.Poweroff)]
    [InlineData(MavState.FlightTermination)]
    public void MapDroneStatus_OfflineStates_ReturnsOffline(MavState state)
    {
        MavStateMapper.MapDroneStatus(state).Should().Be(DroneStatus.Offline);
    }

    // ── MapGpsOk ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(GpsFixType.NoGps, false)]
    [InlineData(GpsFixType.NoFix, false)]
    [InlineData(GpsFixType.Fix2d, false)]
    [InlineData(GpsFixType.Fix3d, true)]
    [InlineData(GpsFixType.Dgps, true)]
    [InlineData(GpsFixType.RtkFloat, true)]
    [InlineData(GpsFixType.RtkFixed, true)]
    [InlineData(GpsFixType.Static_, true)]
    [InlineData(GpsFixType.Ppp, true)]
    public void MapGpsOk_VariousFixTypes_ReturnsExpected(GpsFixType fix, bool expected)
    {
        MavStateMapper.MapGpsOk(fix).Should().Be(expected);
    }

    // ── MapIsArmed ──────────────────────────────────────────────────────────

    [Fact]
    public void MapIsArmed_SafetyArmedBitSet_ReturnsTrue()
    {
        MavStateMapper.MapIsArmed(MavModeFlag.SafetyArmed).Should().BeTrue();
    }

    [Fact]
    public void MapIsArmed_SafetyArmedPluOtherFlags_ReturnsTrue()
    {
        var flags = MavModeFlag.SafetyArmed | MavModeFlag.GuidedEnabled | MavModeFlag.AutoEnabled;
        MavStateMapper.MapIsArmed(flags).Should().BeTrue();
    }

    [Fact]
    public void MapIsArmed_NoBitsSet_ReturnsFalse()
    {
        MavStateMapper.MapIsArmed(0).Should().BeFalse();
    }

    [Fact]
    public void MapIsArmed_OtherFlagsButNotArmed_ReturnsFalse()
    {
        var flags = MavModeFlag.GuidedEnabled | MavModeFlag.AutoEnabled | MavModeFlag.ManualEnabled;
        MavStateMapper.MapIsArmed(flags).Should().BeFalse();
    }
}
