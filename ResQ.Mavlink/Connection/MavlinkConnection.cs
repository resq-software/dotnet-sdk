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

using System.Threading;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;

namespace ResQ.Mavlink.Connection;

/// <summary>
/// High-level MAVLink connection that manages packet sequencing, heartbeat emission,
/// and message dispatch over an <see cref="IMavlinkTransport"/>.
/// </summary>
public sealed class MavlinkConnection : IAsyncDisposable
{
    private readonly IMavlinkTransport _transport;
    private readonly MavlinkConnectionOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _heartbeatTask;
    private int _sequence;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="MavlinkConnection"/> using <see cref="IOptions{T}"/> for DI.
    /// </summary>
    /// <param name="transport">The underlying MAVLink transport.</param>
    /// <param name="options">Connection options wrapped in IOptions.</param>
    public MavlinkConnection(IMavlinkTransport transport, IOptions<MavlinkConnectionOptions> options)
        : this(transport, options.Value)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="MavlinkConnection"/> with raw options.
    /// </summary>
    /// <param name="transport">The underlying MAVLink transport.</param>
    /// <param name="options">Connection options.</param>
    public MavlinkConnection(IMavlinkTransport transport, MavlinkConnectionOptions options)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _heartbeatTask = RunHeartbeatAsync(_cts.Token);
    }

    /// <summary>Gets the MAVLink system ID used by this connection.</summary>
    public byte SystemId => _options.SystemId;

    /// <summary>Gets the MAVLink component ID used by this connection.</summary>
    public byte ComponentId => _options.ComponentId;

    /// <summary>
    /// Wraps <paramref name="message"/> in a <see cref="MavlinkPacket"/> and sends it via the transport.
    /// The sequence number is auto-incremented (wrapping 0-255). The payload is zero-trimmed per
    /// MAVLink v2 specification.
    /// </summary>
    /// <param name="message">The MAVLink message to send.</param>
    /// <param name="ct">Cancellation token.</param>
    public async ValueTask SendMessageAsync(IMavlinkMessage message, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Serialize into max-size scratch buffer then zero-trim (MAVLink v2 spec).
        var scratch = new byte[MavlinkConstants.MaxPayloadLength];
        message.Serialize(scratch);
        var trimmedLen = scratch.Length;
        while (trimmedLen > 0 && scratch[trimmedLen - 1] == 0)
            trimmedLen--;
        var payload = scratch.AsMemory(0, trimmedLen);

        var seq = (byte)(Interlocked.Increment(ref _sequence) & 0xFF);
        var packet = new MavlinkPacket(
            sequenceNumber: seq,
            systemId: _options.SystemId,
            componentId: _options.ComponentId,
            messageId: message.MessageId,
            payload: payload,
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

        await _transport.SendAsync(packet, ct).ConfigureAwait(false);
    }

    private async Task RunHeartbeatAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(_options.HeartbeatInterval, ct).ConfigureAwait(false);

                var hb = new Heartbeat
                {
                    MavlinkVersion = 3,
                };

                try
                {
                    await SendMessageAsync(hb, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Swallow send errors in heartbeat — transport may be gone.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _cts.CancelAsync().ConfigureAwait(false);

        try
        {
            await _heartbeatTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected.
        }

        _cts.Dispose();
    }
}
