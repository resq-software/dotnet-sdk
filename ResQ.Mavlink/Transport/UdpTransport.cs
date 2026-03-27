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
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Protocol;

namespace ResQ.Mavlink.Transport;

/// <summary>
/// UDP-based <see cref="IMavlinkTransport"/> that sends and receives MAVLink v2 packets
/// over a connectionless UDP socket.
/// </summary>
public sealed class UdpTransport : IMavlinkTransport
{
    private readonly UdpTransportOptions _options;
    private readonly UdpClient _udpClient;
    private readonly Channel<TransportState> _stateChannel;
    private TransportState _state;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="UdpTransport"/> using <see cref="IOptions{T}"/> for DI.
    /// </summary>
    /// <param name="options">The transport options wrapped in IOptions.</param>
    public UdpTransport(IOptions<UdpTransportOptions> options)
        : this(options.Value)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="UdpTransport"/> with raw options.
    /// </summary>
    /// <param name="options">The transport options.</param>
    public UdpTransport(UdpTransportOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _stateChannel = Channel.CreateUnbounded<TransportState>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });

        _udpClient = new UdpClient();
        _udpClient.Client.ReceiveBufferSize = options.ReceiveBufferSize;
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, options.ListenPort));

        _state = TransportState.Connected;
        _stateChannel.Writer.TryWrite(TransportState.Connected);
    }

    /// <inheritdoc/>
    public TransportState State => _state;

    /// <inheritdoc/>
    public async ValueTask SendAsync(MavlinkPacket packet, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var data = MavlinkCodec.Serialize(packet);
        var endpoint = new IPEndPoint(IPAddress.Parse(_options.RemoteHost), _options.RemotePort);
        await _udpClient.SendAsync(data, data.Length, endpoint).WaitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<MavlinkPacket> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested && !_disposed)
        {
            UdpReceiveResult result;
            try
            {
                result = await _udpClient.ReceiveAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (ObjectDisposedException)
            {
                yield break;
            }
            catch (SocketException)
            {
                yield break;
            }

            if (MavlinkCodec.TryParse(result.Buffer, out var packet) && packet is not null)
            {
                yield return packet;
            }
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TransportState> StateChanges(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in _stateChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            yield return state;
        }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        _disposed = true;
        _state = TransportState.Disposed;
        _stateChannel.Writer.TryWrite(TransportState.Disposed);
        _stateChannel.Writer.Complete();
        _udpClient.Dispose();

        return ValueTask.CompletedTask;
    }
}
