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

public class MessageRegistryTests
{
    [Fact]
    public void TryDeserialize_Heartbeat_Succeeds()
    {
        var hb = new Heartbeat { Type = Enums.MavType.Quadrotor, MavlinkVersion = 3 };
        var buf = new byte[Heartbeat.PayloadSize];
        hb.Serialize(buf);

        var result = MessageRegistry.TryDeserialize(0, buf, out var message);
        result.Should().BeTrue();
        message.Should().BeOfType<Heartbeat>();
        ((Heartbeat)message!).Type.Should().Be(Enums.MavType.Quadrotor);
    }

    [Fact]
    public void TryDeserialize_UnknownMessageId_ReturnsFalse()
    {
        var result = MessageRegistry.TryDeserialize(99999, ReadOnlySpan<byte>.Empty, out _);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRegistered_AllPhase1Messages_ReturnsTrue()
    {
        uint[] phase1Ids = [0, 1, 11, 20, 22, 23, 24, 30, 33, 40, 42, 44, 47, 51, 70, 73, 74, 76, 77, 86, 87, 242, 245, 253];
        foreach (var id in phase1Ids)
        {
            MessageRegistry.IsRegistered(id).Should().BeTrue($"Message ID {id} should be registered");
        }
    }

    [Fact]
    public void TryDeserialize_SpanOverload_Succeeds()
    {
        Span<byte> buf = stackalloc byte[Heartbeat.PayloadSize];
        var hb = new Heartbeat { Type = Enums.MavType.FixedWing, MavlinkVersion = 3 };
        hb.Serialize(buf);

        var result = MessageRegistry.TryDeserialize(0, (ReadOnlySpan<byte>)buf, out var message);
        result.Should().BeTrue();
        ((Heartbeat)message!).Type.Should().Be(Enums.MavType.FixedWing);
    }
}
