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
using Microsoft.Extensions.Options;
using ResQ.Core;
using ResQ.Mavlink.Enums;
using ResQ.Mavlink.Gateway.Gcs;
using ResQ.Mavlink.Gateway.Routing;
using ResQ.Mavlink.Gateway.Tests.Infrastructure;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;
using Xunit;

namespace ResQ.Mavlink.Gateway.Tests;

/// <summary>
/// End-to-end integration tests for <see cref="MavlinkGateway"/> using <see cref="TestTransport"/>.
/// </summary>
public sealed class MavlinkGatewayIntegrationTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static MavlinkPacket SerializeMessage(IMavlinkMessage message, byte systemId = 1)
    {
        // Use full payload size (no trimming) so deserializers can read all fields safely.
        var scratch = new byte[ResQ.Mavlink.Protocol.MavlinkConstants.MaxPayloadLength];
        message.Serialize(scratch);

        return new MavlinkPacket(
            sequenceNumber: 0,
            systemId: systemId,
            componentId: 1,
            messageId: message.MessageId,
            payload: scratch.AsMemory(),
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);
    }

    private static MavlinkGateway CreateGateway(TestTransport transport)
    {
        var gatewayOpts = Options.Create(new MavlinkGatewayOptions
        {
            GatewaySystemId = 255,
            GatewayComponentId = 190,
        });
        var routingOpts = Options.Create(new GatewayRoutingOptions
        {
            TelemetryRateLimitHz = 100, // High limit so tests aren't rate-limited.
            InternalOnlyMessageIds = [],  // Forward everything for test assertions.
        });
        var gcsOpts = Options.Create(new GcsPassthroughOptions
        {
            Enabled = false, // Disable GCS passthrough in integration tests (no real UDP).
        });

        return new MavlinkGateway(transport, gatewayOpts, routingOpts, gcsOpts);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TelemetryFeed_ReceivesPacket_WhenHeartbeatAndPositionInjected()
    {
        var transport = new TestTransport();
        await using var gateway = CreateGateway(transport);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await gateway.StartAsync(cts.Token);

        // Inject a GlobalPositionInt with known position data first so it's available
        // when the telemetry packet is built.
        var position = new GlobalPositionInt
        {
            Lat = 377_749_000,      // 37.7749°
            Lon = -1_224_194_000,   // -122.4194°
            Alt = 150_000,          // 150 m MSL
            RelativeAlt = 100_000,  // 100 m AGL
        };
        transport.InjectPacket(SerializeMessage(position, systemId: 1));

        // Inject a Heartbeat so we have system status.
        var heartbeat = new Heartbeat
        {
            Type = MavType.Quadrotor,
            Autopilot = MavAutopilot.ArduPilotMega,
            BaseMode = MavModeFlag.SafetyArmed | MavModeFlag.CustomModeEnabled,
            SystemStatus = MavState.Active,
            MavlinkVersion = 3,
        };
        transport.InjectPacket(SerializeMessage(heartbeat, systemId: 1));

        // Read from telemetry feed with timeout; find the first packet with non-zero position.
        TelemetryPacket? received = null;
        using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await foreach (var packet in gateway.TelemetryFeed(readCts.Token))
        {
            if (packet.Position.Latitude != 0.0)
            {
                received = packet;
                break;
            }
        }

        received.Should().NotBeNull("a telemetry packet with position data should be received");
        received!.DroneId.Should().Be("mavlink-1");
        received.Position.Latitude.Should().BeApproximately(37.7749, 1e-4);
        received.Position.Longitude.Should().BeApproximately(-122.4194, 1e-4);
        received.Position.Altitude.Should().BeApproximately(150.0, 1e-2);

        await gateway.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SendToVehicleAsync_TransportReceivesCommandPacket()
    {
        var transport = new TestTransport();
        await using var gateway = CreateGateway(transport);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await gateway.StartAsync(cts.Token);

        var command = new CommandLong
        {
            TargetSystem = 1,
            TargetComponent = 1,
            Command = MavCmd.NavReturnToLaunch,
        };

        await gateway.SendToVehicleAsync(systemId: 1, command, cts.Token);

        transport.SentPackets.Should().ContainSingle(p => p.MessageId == 76u,
            "CommandLong has message ID 76");
        transport.SentPackets[0].SystemId.Should().Be(255,
            "gateway sends with its own system ID");

        await gateway.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task BroadcastAsync_TransportReceivesPacket()
    {
        var transport = new TestTransport();
        await using var gateway = CreateGateway(transport);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await gateway.StartAsync(cts.Token);

        var hb = new Heartbeat { MavlinkVersion = 3 };
        await gateway.BroadcastAsync(hb, cts.Token);

        transport.SentPackets.Should().ContainSingle(p => p.MessageId == 0u);

        await gateway.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StateTracker_UpdatedAfterPositionPacketReceived()
    {
        var transport = new TestTransport();
        await using var gateway = CreateGateway(transport);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await gateway.StartAsync(cts.Token);

        var position = new GlobalPositionInt
        {
            Lat = 512_345_000,
            Lon = 44_567_000,
            Alt = 200_000,
            RelativeAlt = 80_000,
        };
        transport.InjectPacket(SerializeMessage(position, systemId: 2));

        // Allow the receive loop to process the packet.
        await Task.Delay(150, cts.Token);

        var state = gateway.StateTracker.GetVehicle(2);
        state.Should().NotBeNull();
        state!.Latitude.Should().BeApproximately(51.2345, 1e-4);

        await gateway.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Gateway_StopAsync_CompletesCleanly()
    {
        var transport = new TestTransport();
        await using var gateway = CreateGateway(transport);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await gateway.StartAsync(cts.Token);

        var stopAct = async () => await gateway.StopAsync(CancellationToken.None);
        await stopAct.Should().NotThrowAsync();
    }
}
