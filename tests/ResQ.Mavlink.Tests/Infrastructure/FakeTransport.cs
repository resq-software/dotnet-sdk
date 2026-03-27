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
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;

namespace ResQ.Mavlink.Tests.Infrastructure;

/// <summary>
/// A controllable fake <see cref="IMavlinkTransport"/> for unit tests.
/// Packets injected via <see cref="InjectPacket"/> appear in <see cref="ReceiveAsync"/>.
/// All packets sent via <see cref="SendAsync"/> are collected in <see cref="SentPackets"/>.
/// </summary>
public sealed class InjectableTransport : IMavlinkTransport
{
    private readonly Channel<MavlinkPacket> _inbound =
        Channel.CreateUnbounded<MavlinkPacket>(new UnboundedChannelOptions { SingleWriter = false });

    private readonly List<MavlinkPacket> _sent = new();
    private readonly Channel<TransportState> _stateChannel = Channel.CreateUnbounded<TransportState>();
    private bool _disposed;

    /// <summary>Gets all packets sent via <see cref="SendAsync"/>.</summary>
    public IReadOnlyList<MavlinkPacket> SentPackets => _sent;

    /// <inheritdoc/>
    public TransportState State { get; private set; } = TransportState.Connected;

    /// <summary>Injects a packet into the receive stream.</summary>
    public void InjectPacket(MavlinkPacket packet) => _inbound.Writer.TryWrite(packet);

    /// <summary>Signals end-of-stream on the receive channel.</summary>
    public void CompleteReceive() => _inbound.Writer.TryComplete();

    /// <inheritdoc/>
    public ValueTask SendAsync(MavlinkPacket packet, CancellationToken ct = default)
    {
        _sent.Add(packet);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<MavlinkPacket> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var pkt in _inbound.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            yield return pkt;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TransportState> StateChanges(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in _stateChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            yield return state;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        _disposed = true;
        State = TransportState.Disposed;
        _inbound.Writer.TryComplete();
        _stateChannel.Writer.TryWrite(TransportState.Disposed);
        _stateChannel.Writer.Complete();
        return ValueTask.CompletedTask;
    }
}
