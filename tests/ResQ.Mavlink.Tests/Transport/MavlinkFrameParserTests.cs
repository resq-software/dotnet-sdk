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
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;
using Xunit;

namespace ResQ.Mavlink.Tests.Transport;

public sealed class MavlinkFrameParserTests
{
    private static MavlinkPacket MakeHeartbeatPacket()
    {
        var hb = new Heartbeat { MavlinkVersion = 3 };
        var payload = new byte[Heartbeat.PayloadSize];
        hb.Serialize(payload);
        return new MavlinkPacket(1, 255, 190, hb.MessageId, payload, 0, 0, null);
    }

    [Fact]
    public void Feed_CompletePacket_ExtractsOne()
    {
        var parser = new MavlinkFrameParser();
        var packet = MakeHeartbeatPacket();
        var bytes = MavlinkCodec.Serialize(packet);

        parser.Feed(bytes, bytes.Length);
        var results = parser.TryExtract();

        results.Should().HaveCount(1);
        results[0].MessageId.Should().Be(0); // HEARTBEAT
    }

    [Fact]
    public void Feed_SplitAcrossTwoCalls_ExtractsOne()
    {
        var parser = new MavlinkFrameParser();
        var packet = MakeHeartbeatPacket();
        var bytes = MavlinkCodec.Serialize(packet);

        var half = bytes.Length / 2;
        parser.Feed(bytes, half);
        var partial = parser.TryExtract();
        partial.Should().BeEmpty("packet not yet complete");

        parser.Feed(bytes, bytes.Length); // re-feed remaining (wraps due to test)
        // Re-feed the second half only
        var parser2 = new MavlinkFrameParser();
        parser2.Feed(bytes, half);
        parser2.TryExtract().Should().BeEmpty();
        parser2.Feed(bytes[half..], bytes.Length - half);
        var results = parser2.TryExtract();
        results.Should().HaveCount(1);
        results[0].MessageId.Should().Be(0);
    }

    [Fact]
    public void Feed_TwoPackets_ExtractsBoth()
    {
        var parser = new MavlinkFrameParser();
        var p1 = MakeHeartbeatPacket();
        var p2 = MakeHeartbeatPacket();
        var b1 = MavlinkCodec.Serialize(p1);
        var b2 = MavlinkCodec.Serialize(p2);

        var combined = new byte[b1.Length + b2.Length];
        b1.CopyTo(combined, 0);
        b2.CopyTo(combined, b1.Length);

        parser.Feed(combined, combined.Length);
        var results = parser.TryExtract();

        results.Should().HaveCount(2);
    }

    [Fact]
    public void Feed_GarbageBeforePacket_SkipsAndParses()
    {
        var parser = new MavlinkFrameParser();
        var packet = MakeHeartbeatPacket();
        var bytes = MavlinkCodec.Serialize(packet);

        var garbage = new byte[] { 0xAB, 0xCD, 0x00 };
        var combined = new byte[garbage.Length + bytes.Length];
        garbage.CopyTo(combined, 0);
        bytes.CopyTo(combined, garbage.Length);

        parser.Feed(combined, combined.Length);
        var results = parser.TryExtract();

        results.Should().HaveCount(1);
        results[0].MessageId.Should().Be(0);
    }

    [Fact]
    public void Reset_ClearsBuffer()
    {
        var parser = new MavlinkFrameParser();
        var packet = MakeHeartbeatPacket();
        var bytes = MavlinkCodec.Serialize(packet);

        parser.Feed(bytes, bytes.Length / 2);
        parser.Reset();
        parser.Feed(bytes, bytes.Length);
        var results = parser.TryExtract();

        results.Should().HaveCount(1);
    }
}
