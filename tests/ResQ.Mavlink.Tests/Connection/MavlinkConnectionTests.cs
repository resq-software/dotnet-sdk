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

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using FluentAssertions;
using ResQ.Mavlink.Connection;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;
using Xunit;

namespace ResQ.Mavlink.Tests.Connection;

/// <summary>
/// Hand-written fake transport that records all sent packets for assertion.
/// </summary>
internal sealed class FakeTransport : IMavlinkTransport
{
    private readonly List<MavlinkPacket> _sent = new();
    private readonly Channel<TransportState> _stateChannel = Channel.CreateUnbounded<TransportState>();
    private bool _disposed;

    public IReadOnlyList<MavlinkPacket> SentPackets => _sent;

    public TransportState State { get; private set; } = TransportState.Connected;

    public ValueTask SendAsync(MavlinkPacket packet, CancellationToken ct = default)
    {
        _sent.Add(packet);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<MavlinkPacket> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Fake transport never receives anything; just wait until cancelled.
        await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
        yield break;
    }

    public async IAsyncEnumerable<TransportState> StateChanges(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in _stateChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            yield return state;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        _disposed = true;
        State = TransportState.Disposed;
        _stateChannel.Writer.TryWrite(TransportState.Disposed);
        _stateChannel.Writer.Complete();
        return ValueTask.CompletedTask;
    }
}

public sealed class MavlinkConnectionTests
{
    [Fact]
    public void Constructor_SetsSystemIdAndComponentId()
    {
        var transport = new FakeTransport();
        var opts = new MavlinkConnectionOptions { SystemId = 42, ComponentId = 99 };

        using var cts = new CancellationTokenSource();
        var connection = new MavlinkConnection(transport, opts);

        connection.SystemId.Should().Be(42);
        connection.ComponentId.Should().Be(99);
    }

    [Fact]
    public async Task SendMessageAsync_CommandLong_TransportReceivesPacketWithCorrectMessageId()
    {
        var transport = new FakeTransport();
        var opts = new MavlinkConnectionOptions
        {
            SystemId = 255,
            ComponentId = 190,
            // Use a long heartbeat interval so it doesn't interfere with the assertion.
            HeartbeatInterval = TimeSpan.FromMinutes(10),
        };

        await using var connection = new MavlinkConnection(transport, opts);

        var command = new CommandLong
        {
            TargetSystem = 1,
            TargetComponent = 1,
            Command = ResQ.Mavlink.Enums.MavCmd.NavWaypoint,
            Param1 = 0f,
        };

        await connection.SendMessageAsync(command);

        transport.SentPackets.Should().HaveCount(1);
        transport.SentPackets[0].MessageId.Should().Be(76u); // COMMAND_LONG
        transport.SentPackets[0].SystemId.Should().Be(255);
        transport.SentPackets[0].ComponentId.Should().Be(190);
    }

    [Fact]
    public async Task SendMessageAsync_SequenceAutoIncrements()
    {
        var transport = new FakeTransport();
        var opts = new MavlinkConnectionOptions
        {
            HeartbeatInterval = TimeSpan.FromMinutes(10),
        };

        await using var connection = new MavlinkConnection(transport, opts);

        var hb = new Heartbeat { MavlinkVersion = 3 };
        await connection.SendMessageAsync(hb);
        await connection.SendMessageAsync(hb);

        transport.SentPackets.Should().HaveCount(2);
        transport.SentPackets[1].SequenceNumber.Should()
            .Be((byte)(transport.SentPackets[0].SequenceNumber + 1));
    }
}
