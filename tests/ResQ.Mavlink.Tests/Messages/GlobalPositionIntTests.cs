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
using ResQ.Mavlink.Messages;
using Xunit;

namespace ResQ.Mavlink.Tests.Messages;

public class GlobalPositionIntTests
{
    [Fact]
    public void Serialize_ThenDeserialize_RoundTrips()
    {
        var original = new GlobalPositionInt
        {
            TimeBootMs = 12345,
            Lat = 473977418,    // 47.3977418° (Zurich)
            Lon = 85255792,     // 8.5255792°
            Alt = 408000,       // 408m in mm
            RelativeAlt = 50000, // 50m in mm
            Vx = 100,           // 1.0 m/s (cm/s)
            Vy = -50,
            Vz = 10,
            Hdg = 18000,        // 180.00°
        };

        Span<byte> buffer = stackalloc byte[GlobalPositionInt.PayloadSize];
        original.Serialize(buffer);

        var parsed = GlobalPositionInt.Deserialize(buffer);
        parsed.Lat.Should().Be(473977418);
        parsed.Lon.Should().Be(85255792);
        parsed.Alt.Should().Be(408000);
        parsed.RelativeAlt.Should().Be(50000);
        parsed.Vx.Should().Be(100);
        parsed.Vy.Should().Be(-50);
        parsed.Hdg.Should().Be(18000);
    }

    [Fact]
    public void MessageId_Is33()
    {
        var msg = new GlobalPositionInt();
        msg.MessageId.Should().Be(33u);
    }
}
