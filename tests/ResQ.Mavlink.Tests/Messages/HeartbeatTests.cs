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

public class HeartbeatTests
{
    [Fact]
    public void Serialize_ThenDeserialize_RoundTrips()
    {
        var original = new Heartbeat
        {
            Type = MavType.Quadrotor,
            Autopilot = MavAutopilot.ArduPilotMega,
            BaseMode = MavModeFlag.CustomModeEnabled | MavModeFlag.SafetyArmed,
            CustomMode = 5,
            SystemStatus = MavState.Active,
            MavlinkVersion = 3,
        };

        Span<byte> buffer = stackalloc byte[Heartbeat.PayloadSize];
        original.Serialize(buffer);

        var parsed = Heartbeat.Deserialize(buffer);
        parsed.Type.Should().Be(MavType.Quadrotor);
        parsed.Autopilot.Should().Be(MavAutopilot.ArduPilotMega);
        parsed.CustomMode.Should().Be(5u);
        parsed.SystemStatus.Should().Be(MavState.Active);
        parsed.MavlinkVersion.Should().Be(3);
    }

    [Fact]
    public void MessageId_IsZero()
    {
        var hb = new Heartbeat();
        hb.MessageId.Should().Be(0u);
    }
}
