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

public sealed class UdpTransportTests
{
    /// <summary>Receives a single packet from <paramref name="transport"/> and returns it.</summary>
    private static async Task<MavlinkPacket> ReceiveOneAsync(
        IMavlinkTransport transport,
        CancellationToken ct)
    {
        await foreach (var p in transport.ReceiveAsync(ct).ConfigureAwait(false))
            return p;

        throw new InvalidOperationException("No packet received before cancellation.");
    }

    [Fact]
    public async Task State_AfterConstruction_IsConnected()
    {
        var opts = new UdpTransportOptions { ListenPort = 14582, RemotePort = 14583, RemoteHost = "127.0.0.1" };
        await using var transport = new UdpTransport(opts);

        transport.State.Should().Be(TransportState.Connected);
    }

    [Fact]
    public async Task State_AfterDispose_IsDisposed()
    {
        var opts = new UdpTransportOptions { ListenPort = 14584, RemotePort = 14585, RemoteHost = "127.0.0.1" };
        var transport = new UdpTransport(opts);

        await transport.DisposeAsync();

        transport.State.Should().Be(TransportState.Disposed);
    }

    [Fact]
    public async Task Loopback_SendAndReceive_PacketMatchesMessageId()
    {
        // Transport A listens on 14580, sends to 14581
        // Transport B listens on 14581, sends to 14580
        var optsA = new UdpTransportOptions { ListenPort = 14580, RemotePort = 14581, RemoteHost = "127.0.0.1" };
        var optsB = new UdpTransportOptions { ListenPort = 14581, RemotePort = 14580, RemoteHost = "127.0.0.1" };

        await using var transportA = new UdpTransport(optsA);
        await using var transportB = new UdpTransport(optsB);

        var hb = new Heartbeat { MavlinkVersion = 3 };
        var payload = new byte[Heartbeat.PayloadSize];
        hb.Serialize(payload);

        var sendPacket = new MavlinkPacket(
            sequenceNumber: 1,
            systemId: 255,
            componentId: 190,
            messageId: hb.MessageId,
            payload: payload,
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Start receive on B before sending from A.
        var receiveTask = ReceiveOneAsync(transportB, cts.Token);

        // Give receiver a moment to begin waiting.
        await Task.Delay(50, cts.Token);

        await transportA.SendAsync(sendPacket, cts.Token);

        var received = await receiveTask;

        received.MessageId.Should().Be(hb.MessageId);
        received.SystemId.Should().Be(255);
        received.ComponentId.Should().Be(190);
        received.SequenceNumber.Should().Be(1);
    }
}
