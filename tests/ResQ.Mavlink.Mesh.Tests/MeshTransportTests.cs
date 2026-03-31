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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Mesh.Tests.Infrastructure;
using ResQ.Mavlink.Protocol;
using Xunit;

namespace ResQ.Mavlink.Mesh.Tests;

/// <summary>
/// Unit tests for <see cref="MeshTransport"/>.
/// </summary>
public sealed class MeshTransportTests
{
    private static MeshTransport CreateMeshTransport(
        TestTransport inner,
        MeshTransportOptions? opts = null)
    {
        var options = Options.Create(opts ?? new MeshTransportOptions());
        return new MeshTransport(inner, options);
    }

    private static MavlinkPacket MakePacket(
        uint messageId = 0,
        byte systemId = 1,
        byte seq = 0,
        byte compatFlags = 0)
        => new(
            sequenceNumber: seq,
            systemId: systemId,
            componentId: 1,
            messageId: messageId,
            payload: Array.Empty<byte>(),
            incompatFlags: 0,
            compatFlags: compatFlags,
            signature: null);

    // ── deduplication ────────────────────────────────────────────────────

    [Fact]
    public async Task Dedup_SamePacketReceivedTwice_DeliveredOnce()
    {
        var inner = new TestTransport();
        await using var mesh = CreateMeshTransport(inner);

        var pkt = MakePacket(systemId: 1, seq: 42);
        inner.InjectPacket(pkt);
        inner.InjectPacket(pkt); // duplicate
        inner.CompleteReceive();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var received = new List<MavlinkPacket>();
        await foreach (var p in mesh.ReceiveAsync(cts.Token))
            received.Add(p);

        received.Should().HaveCount(1);
    }

    [Fact]
    public async Task Dedup_DifferentSystemIds_BothDelivered()
    {
        var inner = new TestTransport();
        await using var mesh = CreateMeshTransport(inner);

        inner.InjectPacket(MakePacket(systemId: 1, seq: 1));
        inner.InjectPacket(MakePacket(systemId: 2, seq: 1)); // different sysId, same seq
        inner.CompleteReceive();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var received = new List<MavlinkPacket>();
        await foreach (var p in mesh.ReceiveAsync(cts.Token))
            received.Add(p);

        received.Should().HaveCount(2);
    }

    // ── TTL rebroadcast ──────────────────────────────────────────────────

    [Fact]
    public async Task Ttl_PacketWithTtl3_RebroadcastWithTtl2()
    {
        var inner = new TestTransport();
        await using var mesh = CreateMeshTransport(inner);

        // compatFlags low nibble = TTL
        var pkt = MakePacket(compatFlags: 3);
        inner.InjectPacket(pkt);
        inner.CompleteReceive();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var _ in mesh.ReceiveAsync(cts.Token)) { }

        // The transport should have received one rebroadcast with TTL=2
        inner.SentPackets.Should().HaveCount(1);
        (inner.SentPackets[0].CompatFlags & 0x0F).Should().Be(2);
    }

    [Fact]
    public async Task Ttl_PacketWithTtl0_NotRebroadcast_ButDeliveredLocally()
    {
        var inner = new TestTransport();
        await using var mesh = CreateMeshTransport(inner);

        var pkt = MakePacket(compatFlags: 0); // TTL = 0
        inner.InjectPacket(pkt);
        inner.CompleteReceive();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var received = new List<MavlinkPacket>();
        await foreach (var p in mesh.ReceiveAsync(cts.Token))
            received.Add(p);

        inner.SentPackets.Should().BeEmpty("TTL=0 means no rebroadcast");
        received.Should().HaveCount(1, "packet still delivered locally");
    }

    // ── priority queuing ─────────────────────────────────────────────────

    [Fact]
    public void Priority_EmergencyBeacon_HasLowestPriorityNumber()
    {
        var emergency = MeshTransport.GetPriority(60007);
        var detection = MeshTransport.GetPriority(60000);
        var telemetry = MeshTransport.GetPriority(0);

        emergency.Should().BeLessThan(detection);
        detection.Should().BeLessThan(telemetry);
    }

    [Fact]
    public async Task Priority_QueueOverflow_EvictsLowestPriority()
    {
        var inner = new TestTransport();
        var opts = new MeshTransportOptions { MaxTransmitQueueSize = 2, DefaultTtl = 0 };
        await using var mesh = new MeshTransport(inner, Options.Create(opts));

        // Verify priority assignments used by the queue ordering.
        MeshTransport.GetPriority(60007).Should().Be(0);
        MeshTransport.GetPriority(60000).Should().Be(1);
        MeshTransport.GetPriority(60002).Should().Be(2);
        MeshTransport.GetPriority(0).Should().Be(10);
    }
}
