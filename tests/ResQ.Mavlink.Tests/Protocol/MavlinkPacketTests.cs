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

public class MavlinkPacketTests
{
    [Fact]
    public void Constructor_SetsAllFields()
    {
        byte[] payload = [0x01, 0x02, 0x03];
        var packet = new MavlinkPacket(
            sequenceNumber: 42,
            systemId: 1,
            componentId: 1,
            messageId: 0,
            payload: payload,
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

        packet.SequenceNumber.Should().Be(42);
        packet.SystemId.Should().Be(1);
        packet.ComponentId.Should().Be(1);
        packet.MessageId.Should().Be(0u);
        packet.Payload.Should().BeEquivalentTo(payload);
        packet.IsSigned.Should().BeFalse();
    }

    [Fact]
    public void IsSigned_WithSignature_ReturnsTrue()
    {
        var packet = new MavlinkPacket(0, 1, 1, 0, ReadOnlyMemory<byte>.Empty,
            incompatFlags: MavlinkConstants.IncompatFlagSigned,
            compatFlags: 0,
            signature: new byte[MavlinkConstants.SignatureLength]);

        packet.IsSigned.Should().BeTrue();
    }

    [Fact]
    public void PayloadLength_ReturnsCorrectLength()
    {
        byte[] payload = [0x01, 0x02, 0x03, 0x04, 0x05];
        var packet = new MavlinkPacket(0, 1, 1, 33, payload, 0, 0, null);
        packet.PayloadLength.Should().Be(5);
    }
}
