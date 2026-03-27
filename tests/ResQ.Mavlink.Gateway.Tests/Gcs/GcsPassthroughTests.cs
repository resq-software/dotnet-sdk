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
using ResQ.Mavlink.Gateway.Gcs;
using ResQ.Mavlink.Gateway.Tests.Infrastructure;
using ResQ.Mavlink.Protocol;
using Xunit;

namespace ResQ.Mavlink.Gateway.Tests.Gcs;

file static class TestPackets
{
    /// <summary>Creates a minimal packet with the specified message ID.</summary>
    public static MavlinkPacket Make(uint messageId, byte systemId = 1) =>
        new(
            sequenceNumber: 0,
            systemId: systemId,
            componentId: 1,
            messageId: messageId,
            payload: new byte[1],
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);
}

public sealed class GcsPassthroughTests
{
    private static GcsPassthroughOptions DefaultOptions(bool resqPriority = false) =>
        new() { Enabled = true, ResqPriorityOverride = resqPriority };

    // -- GCS → Vehicle forwarding --------------------------------------------

    [Fact]
    public async Task GcsPacket_IsForwardedToVehicle()
    {
        var vehicle = new TestTransport();
        var gcs = new TestTransport();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var passthrough = new GcsPassthrough(vehicle, gcs, DefaultOptions());
        await passthrough.StartAsync(cts.Token);

        var packet = TestPackets.Make(messageId: 33); // GlobalPositionInt — not a command
        gcs.InjectPacket(packet);

        // Allow the forwarding loop to pick it up.
        await Task.Delay(100, cts.Token);

        vehicle.SentPackets.Should().ContainSingle(p => p.MessageId == 33u);
    }

    // -- Vehicle → GCS forwarding via ForwardToGcsAsync ----------------------

    [Fact]
    public async Task ForwardToGcsAsync_SendsPacketToGcsTransport()
    {
        var vehicle = new TestTransport();
        var gcs = new TestTransport();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var passthrough = new GcsPassthrough(vehicle, gcs, DefaultOptions());
        await passthrough.StartAsync(cts.Token);

        var packet = TestPackets.Make(messageId: 0); // Heartbeat
        await passthrough.ForwardToGcsAsync(packet, cts.Token);

        gcs.SentPackets.Should().ContainSingle(p => p.MessageId == 0u);
    }

    // -- Packets are not modified during forwarding --------------------------

    [Fact]
    public async Task ForwardedPacket_IsNotModified()
    {
        var vehicle = new TestTransport();
        var gcs = new TestTransport();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var passthrough = new GcsPassthrough(vehicle, gcs, DefaultOptions());
        await passthrough.StartAsync(cts.Token);

        var original = TestPackets.Make(messageId: 33, systemId: 7);
        await passthrough.ForwardToGcsAsync(original, cts.Token);

        var forwarded = gcs.SentPackets.Should().ContainSingle().Subject;
        forwarded.MessageId.Should().Be(original.MessageId);
        forwarded.SystemId.Should().Be(original.SystemId);
    }

    // -- ResQ priority: command packets are dropped after NotifyResqCommand --

    [Fact]
    public async Task ResqPriority_CommandLong_DroppedWithinWindow()
    {
        var vehicle = new TestTransport();
        var gcs = new TestTransport();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var opts = new GcsPassthroughOptions
        {
            Enabled = true,
            ResqPriorityOverride = true,
        };

        await using var passthrough = new GcsPassthrough(vehicle, gcs, opts);
        await passthrough.StartAsync(cts.Token);

        // Notify that ResQ just sent a command.
        passthrough.NotifyResqCommand();

        // Now inject a GCS CommandLong (ID 76) — should be dropped.
        gcs.InjectPacket(TestPackets.Make(messageId: 76));

        await Task.Delay(150, cts.Token);

        vehicle.SentPackets.Should().BeEmpty("GCS command should be suppressed during ResQ priority window");
    }

    [Fact]
    public async Task ResqPriority_NonCommandPacket_IsNotDropped()
    {
        var vehicle = new TestTransport();
        var gcs = new TestTransport();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var opts = new GcsPassthroughOptions
        {
            Enabled = true,
            ResqPriorityOverride = true,
        };

        await using var passthrough = new GcsPassthrough(vehicle, gcs, opts);
        await passthrough.StartAsync(cts.Token);

        passthrough.NotifyResqCommand();

        // Non-command packet (GlobalPositionInt = 33) should still pass through.
        gcs.InjectPacket(TestPackets.Make(messageId: 33));

        await Task.Delay(150, cts.Token);

        vehicle.SentPackets.Should().ContainSingle(p => p.MessageId == 33u);
    }
}
