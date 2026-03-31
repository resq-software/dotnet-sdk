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

public class MavlinkCodecTests
{
    [Fact]
    public void Serialize_ThenParse_RoundTrips()
    {
        byte[] payload = [0x00, 0x00, 0x00, 0x00, 0x02, 0x03, 0x51, 0x04, 0x03];
        var original = new MavlinkPacket(
            sequenceNumber: 1,
            systemId: 1,
            componentId: 1,
            messageId: 0, // HEARTBEAT
            payload: payload,
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

        var bytes = MavlinkCodec.Serialize(original);
        var parsed = MavlinkCodec.TryParse(bytes, out var result);

        parsed.Should().BeTrue();
        result.Should().NotBeNull();
        result!.SystemId.Should().Be(1);
        result.ComponentId.Should().Be(1);
        result.MessageId.Should().Be(0u);
        result.Payload.Should().BeEquivalentTo(payload);
        result.SequenceNumber.Should().Be(1);
    }

    [Fact]
    public void TryParse_TooShortBuffer_ReturnsFalse()
    {
        byte[] tooShort = [MavlinkConstants.StxV2, 0x00, 0x00];
        MavlinkCodec.TryParse(tooShort, out var result).Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryParse_WrongStx_ReturnsFalse()
    {
        var bytes = new byte[MavlinkConstants.MinPacketLength];
        bytes[0] = 0xAA; // wrong STX
        MavlinkCodec.TryParse(bytes, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_CorruptedCrc_ReturnsFalse()
    {
        byte[] payload = [0x00, 0x00, 0x00, 0x00, 0x02, 0x03, 0x51, 0x04, 0x03];
        var original = new MavlinkPacket(1, 1, 1, 0, payload, 0, 0, null);
        var bytes = MavlinkCodec.Serialize(original);

        // Corrupt the last byte (CRC)
        bytes[^1] ^= 0xFF;

        MavlinkCodec.TryParse(bytes, out _).Should().BeFalse();
    }

    [Fact]
    public void Serialize_SetsStxByte()
    {
        var packet = new MavlinkPacket(0, 1, 1, 0, new byte[] { 0x00 }, 0, 0, null);
        var bytes = MavlinkCodec.Serialize(packet);
        bytes[0].Should().Be(MavlinkConstants.StxV2);
    }

    [Fact]
    public void Serialize_PayloadLength_EncodedInHeader()
    {
        byte[] payload = [0x01, 0x02, 0x03, 0x04, 0x05];
        var packet = new MavlinkPacket(0, 1, 1, 33, payload, 0, 0, null);
        var bytes = MavlinkCodec.Serialize(packet);
        bytes[1].Should().Be(5); // payload length byte
    }

    [Fact]
    public void Serialize_UnknownMessageId_Throws()
    {
        byte[] payload = [0x01, 0x02];
        var packet = new MavlinkPacket(0, 1, 1, 59999, payload, 0, 0, null);
        var act = () => MavlinkCodec.Serialize(packet);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TryParse_RandomBytes_NeverThrows()
    {
        var rng = new Random(42);
        for (var i = 0; i < 1000; i++)
        {
            var len = rng.Next(0, 300);
            var bytes = new byte[len];
            rng.NextBytes(bytes);

            // Must never throw — only return true/false
            var act = () => MavlinkCodec.TryParse(bytes, out _);
            act.Should().NotThrow();
        }
    }
}
