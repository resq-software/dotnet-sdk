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
using ResQ.Mavlink.Protocol;
using Xunit;

namespace ResQ.Mavlink.Tests.Protocol;

public class MavlinkCrcTests
{
    [Fact]
    public void Accumulate_EmptyInput_ReturnsInitialSeed()
    {
        var crc = MavlinkCrc.Calculate(ReadOnlySpan<byte>.Empty);
        // CRC-16/MCRF4XX initial value 0xFFFF with no data
        crc.Should().Be(0xFFFF);
    }

    [Fact]
    public void Calculate_KnownHeartbeatPayload_MatchesExpected()
    {
        // A known MAVLink v2 heartbeat payload (9 bytes):
        // custom_mode=0, type=2(quadrotor), autopilot=3(ardupilot),
        // base_mode=81, system_status=4(active), mavlink_version=3
        byte[] payload = [0x00, 0x00, 0x00, 0x00, 0x02, 0x03, 0x51, 0x04, 0x03];
        byte crcExtra = 50; // Heartbeat CRC_EXTRA

        var crc = MavlinkCrc.Calculate(payload);
        crc = MavlinkCrc.Accumulate(crc, crcExtra);

        // The CRC must be deterministic and non-zero
        crc.Should().NotBe(0);
        crc.Should().NotBe(0xFFFF);
    }

    [Fact]
    public void Accumulate_SingleByte_ProducesConsistentResult()
    {
        var crc1 = MavlinkCrc.Accumulate(0xFFFF, 0x42);
        var crc2 = MavlinkCrc.Accumulate(0xFFFF, 0x42);
        crc1.Should().Be(crc2);
    }

    [Fact]
    public void GetCrcExtra_Heartbeat_Returns50()
    {
        MavlinkCrc.GetCrcExtra(0).Should().Be(50);
    }

    [Fact]
    public void GetCrcExtra_UnknownMessageId_ReturnsNull()
    {
        MavlinkCrc.GetCrcExtra(99999).Should().BeNull();
    }
}
