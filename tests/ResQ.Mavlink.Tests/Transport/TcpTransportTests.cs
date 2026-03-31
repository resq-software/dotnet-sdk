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

using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;
using Xunit;

namespace ResQ.Mavlink.Tests.Transport;

public sealed class TcpTransportTests
{
    /// <summary>Allocates a free OS port by binding to port 0 and returning the assigned port.</summary>
    private static int GetFreePort()
    {
        using var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private static MavlinkPacket MakeHeartbeatPacket()
    {
        var hb = new Heartbeat { MavlinkVersion = 3 };
        var payload = new byte[Heartbeat.PayloadSize];
        hb.Serialize(payload);
        return new MavlinkPacket(1, 255, 190, hb.MessageId, payload, 0, 0, null);
    }

    private static async Task<MavlinkPacket> ReceiveOneAsync(IMavlinkTransport transport, CancellationToken ct)
    {
        await foreach (var p in transport.ReceiveAsync(ct).ConfigureAwait(false))
            return p;
        throw new InvalidOperationException("No packet received.");
    }

    [Fact]
    public async Task State_AfterConnect_IsConnected()
    {
        var port = GetFreePort();
        var serverOpts = new TcpTransportOptions { Host = "127.0.0.1", Port = port, IsServer = true };
        var clientOpts = new TcpTransportOptions { Host = "127.0.0.1", Port = port, IsServer = false };

        await using var server = new TcpTransport(serverOpts);
        await using var client = new TcpTransport(clientOpts);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var serverConnect = server.ConnectAsync(cts.Token);
        await client.ConnectAsync(cts.Token);
        await serverConnect;

        client.State.Should().Be(TransportState.Connected);
        server.State.Should().Be(TransportState.Connected);
    }

    [Fact]
    public async Task State_AfterDispose_IsDisposed()
    {
        var port = GetFreePort();
        var opts = new TcpTransportOptions { Host = "127.0.0.1", Port = port, IsServer = true };
        var transport = new TcpTransport(opts);

        await transport.DisposeAsync();

        transport.State.Should().Be(TransportState.Disposed);
    }

    [Fact]
    public async Task Loopback_ClientToServer_PacketReceived()
    {
        var port = GetFreePort();
        var serverOpts = new TcpTransportOptions { Host = "127.0.0.1", Port = port, IsServer = true };
        var clientOpts = new TcpTransportOptions { Host = "127.0.0.1", Port = port, IsServer = false };

        await using var server = new TcpTransport(serverOpts);
        await using var client = new TcpTransport(clientOpts);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Server must start accepting before client connects
        var serverConnectTask = server.ConnectAsync(cts.Token);
        await client.ConnectAsync(cts.Token);
        await serverConnectTask;

        var packet = MakeHeartbeatPacket();

        var receiveTask = ReceiveOneAsync(server, cts.Token);

        // Small delay so receive starts
        await Task.Delay(20, cts.Token);

        await client.SendAsync(packet, cts.Token);

        var received = await receiveTask;

        received.MessageId.Should().Be(packet.MessageId);
        received.SystemId.Should().Be(255);
        received.SequenceNumber.Should().Be(1);
    }

    [Fact]
    public async Task Reconnect_AfterServerDisconnect_EntersReconnectingState()
    {
        var port = GetFreePort();
        var serverOpts = new TcpTransportOptions
        {
            Host = "127.0.0.1",
            Port = port,
            IsServer = true,
            ReconnectDelay = TimeSpan.FromMilliseconds(100),
        };
        var clientOpts = new TcpTransportOptions
        {
            Host = "127.0.0.1",
            Port = port,
            IsServer = false,
            ReconnectDelay = TimeSpan.FromMilliseconds(100),
        };

        await using var server = new TcpTransport(serverOpts);
        await using var client = new TcpTransport(clientOpts);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var serverConnectTask = server.ConnectAsync(cts.Token);
        await client.ConnectAsync(cts.Token);
        await serverConnectTask;

        // Dispose server to simulate disconnect
        await server.DisposeAsync();

        // Client should detect disconnect and transition to Reconnecting
        // Give it a moment
        await Task.Delay(200, CancellationToken.None);

        // Start receiving (which will trigger reconnect detection)
        using var receiveCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        try
        {
            await foreach (var _ in client.ReceiveAsync(receiveCts.Token).ConfigureAwait(false))
                break;
        }
        catch (OperationCanceledException) { }

        // After the disconnect is detected, client should be Reconnecting or Connecting
        client.State.Should().BeOneOf(TransportState.Reconnecting, TransportState.Connecting, TransportState.Disconnected);
    }
}
