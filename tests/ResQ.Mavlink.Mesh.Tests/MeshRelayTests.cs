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

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Mesh.Tests.Infrastructure;
using ResQ.Mavlink.Protocol;
using Xunit;

namespace ResQ.Mavlink.Mesh.Tests;

/// <summary>
/// Unit tests for <see cref="MeshRelay"/>.
/// </summary>
public sealed class MeshRelayTests
{
    private static MeshRelay CreateRelay(MeshRelayOptions? opts = null)
        => new(Options.Create(opts ?? new MeshRelayOptions()));

    private static MavlinkPacket MakePacket(uint messageId = 0, byte seq = 0)
        => new(
            sequenceNumber: seq,
            systemId: 1,
            componentId: 1,
            messageId: messageId,
            payload: Array.Empty<byte>(),
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

    [Fact]
    public void BufferMessage_IncreasesBufferedCount()
    {
        var relay = CreateRelay();
        relay.BufferMessage(MakePacket(), priority: 5);
        relay.BufferMessage(MakePacket(seq: 1), priority: 5);
        relay.BufferedCount.Should().Be(2);
        relay.IsBuffering.Should().BeTrue();
    }

    [Fact]
    public async Task FlushAsync_SendsAllBufferedPackets_AndClearsBuffer()
    {
        var relay = CreateRelay();
        relay.BufferMessage(MakePacket(seq: 0), priority: 5);
        relay.BufferMessage(MakePacket(seq: 1), priority: 3);
        relay.BufferMessage(MakePacket(seq: 2), priority: 1);

        var ground = new TestTransport();
        await relay.FlushAsync(ground);

        ground.SentPackets.Should().HaveCount(3);
        relay.BufferedCount.Should().Be(0);
        relay.IsBuffering.Should().BeFalse();
    }

    [Fact]
    public async Task FlushAsync_SendsHigherPriorityFirst()
    {
        var relay = CreateRelay();
        relay.BufferMessage(MakePacket(messageId: 0, seq: 10), priority: 10); // low prio
        relay.BufferMessage(MakePacket(messageId: 60007, seq: 11), priority: 0); // emergency

        var ground = new TestTransport();
        await relay.FlushAsync(ground);

        // First sent should be the emergency (priority 0)
        ground.SentPackets[0].MessageId.Should().Be(60007);
        ground.SentPackets[1].MessageId.Should().Be(0);
    }

    [Fact]
    public async Task BufferFull_HigherPriorityArrives_EvictsLowestPriority()
    {
        var relay = CreateRelay(new MeshRelayOptions { MaxBufferSize = 2, PriorityEviction = true });
        relay.BufferMessage(MakePacket(seq: 0), priority: 10);
        relay.BufferMessage(MakePacket(seq: 1), priority: 8);
        relay.BufferedCount.Should().Be(2);

        // Add higher-priority (lower number) message
        relay.BufferMessage(MakePacket(messageId: 60007, seq: 2), priority: 0);
        relay.BufferedCount.Should().Be(2, "buffer size unchanged after eviction");

        // One of the original low-priority messages should have been evicted
        // The emergency one should still be there
        // We can't directly inspect internals, but flush to verify
        var ground = new TestTransport();
        await relay.FlushAsync(ground);
        ground.SentPackets.Should().Contain(p => p.MessageId == 60007);
    }

    [Fact]
    public void BufferFull_LowerOrEqualPriorityArrives_NewMessageDropped()
    {
        var relay = CreateRelay(new MeshRelayOptions { MaxBufferSize = 2, PriorityEviction = true });
        relay.BufferMessage(MakePacket(seq: 0), priority: 1);
        relay.BufferMessage(MakePacket(seq: 1), priority: 1);

        // Add same or worse priority — should be dropped
        relay.BufferMessage(MakePacket(seq: 2), priority: 5);
        relay.BufferedCount.Should().Be(2, "new lower-priority message was dropped");
    }

    [Fact]
    public void BufferFull_PriorityEvictionDisabled_NewMessageDropped()
    {
        var relay = CreateRelay(new MeshRelayOptions { MaxBufferSize = 1, PriorityEviction = false });
        relay.BufferMessage(MakePacket(seq: 0), priority: 10);
        relay.BufferMessage(MakePacket(messageId: 60007, seq: 1), priority: 0); // emergency but dropped

        relay.BufferedCount.Should().Be(1);
    }
}
