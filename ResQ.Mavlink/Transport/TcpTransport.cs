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

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Protocol;

namespace ResQ.Mavlink.Transport;

/// <summary>
/// TCP-based <see cref="IMavlinkTransport"/> that supports both client and server modes.
/// Uses length-prefixed framing: 4-byte little-endian length followed by the MAVLink packet bytes.
/// Automatically attempts reconnection with exponential backoff on disconnect.
/// </summary>
public sealed class TcpTransport : IMavlinkTransport
{
    private readonly TcpTransportOptions _options;
    private readonly Channel<TransportState> _stateChannel;
    private readonly Channel<byte[]> _reconnectBuffer;
    private readonly SemaphoreSlim _streamLock = new(1, 1);
    private TcpClient? _tcpClient;
    private TcpListener? _listener;
    private NetworkStream? _stream;
    private TransportState _state;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="TcpTransport"/> using <see cref="IOptions{T}"/> for DI.
    /// </summary>
    /// <param name="options">Transport options wrapped in IOptions.</param>
    public TcpTransport(IOptions<TcpTransportOptions> options)
        : this(options.Value)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="TcpTransport"/> with raw options.
    /// </summary>
    /// <param name="options">Transport options.</param>
    public TcpTransport(TcpTransportOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _stateChannel = Channel.CreateUnbounded<TransportState>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });

        var bufferCapacity = options.MaxReconnectBuffer > 0 ? options.MaxReconnectBuffer : 1;
        _reconnectBuffer = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(bufferCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = false,
            SingleReader = false,
        });

        _state = TransportState.Connecting;
    }

    /// <inheritdoc/>
    public TransportState State => _state;

    /// <summary>
    /// Connects the transport. Must be called before <see cref="SendAsync"/> or <see cref="ReceiveAsync"/>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        SetState(TransportState.Connecting);
        if (_options.IsServer)
        {
            _listener = new TcpListener(IPAddress.Parse(_options.Host), _options.Port);
            _listener.Start();
            _tcpClient = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
        }
        else
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_options.Host, _options.Port, ct).ConfigureAwait(false);
        }
        _stream = _tcpClient.GetStream();
        SetState(TransportState.Connected);
    }

    /// <inheritdoc/>
    public async ValueTask SendAsync(MavlinkPacket packet, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var payload = MavlinkCodec.Serialize(packet);
        var frame = WrapFrame(payload);

        await _streamLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_stream is null || !(_tcpClient?.Connected ?? false))
            {
                if (_options.MaxReconnectBuffer > 0)
                    _reconnectBuffer.Writer.TryWrite(frame);
                return;
            }

            await _stream.WriteAsync(frame, ct).ConfigureAwait(false);
        }
        catch (Exception) when (!_disposed)
        {
            if (_options.MaxReconnectBuffer > 0)
                _reconnectBuffer.Writer.TryWrite(frame);
        }
        finally
        {
            _streamLock.Release();
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<MavlinkPacket> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var parser = new MavlinkFrameParser();
        var header = new byte[4];
        var backoff = TimeSpan.FromMilliseconds(500);

        while (!ct.IsCancellationRequested && !_disposed)
        {
            if (_stream is null)
            {
                await Task.Delay(50, ct).ConfigureAwait(false);
                continue;
            }

            MavlinkPacket? packet = null;
            try
            {
                // Read 4-byte length prefix
                var read = await ReadExactAsync(_stream, header, 4, ct).ConfigureAwait(false);
                if (read < 4)
                    throw new IOException("Stream closed");

                var frameLen = BinaryPrimitives.ReadInt32LittleEndian(header);
                if (frameLen <= 0 || frameLen > 300)
                    throw new IOException($"Invalid frame length {frameLen}");

                var frameData = new byte[frameLen];
                read = await ReadExactAsync(_stream, frameData, frameLen, ct).ConfigureAwait(false);
                if (read < frameLen)
                    throw new IOException("Stream closed");

                if (MavlinkCodec.TryParse(frameData, out packet) && packet is not null)
                {
                    backoff = TimeSpan.FromMilliseconds(500);
                }
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (Exception) when (!_disposed)
            {
                // Disconnect — attempt reconnect
                SetState(TransportState.Reconnecting);
                parser.Reset();
                CloseCurrentConnection();

                await Task.Delay(backoff, ct).ConfigureAwait(false);
                backoff = TimeSpan.FromMilliseconds(Math.Min(backoff.TotalMilliseconds * 2, 30_000));

                try
                {
                    await ReconnectAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }
                catch
                {
                    // Will retry on next loop
                }
                continue;
            }

            if (packet is not null)
                yield return packet;
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
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        SetState(TransportState.Disposed);
        _stateChannel.Writer.Complete();
        _reconnectBuffer.Writer.Complete();

        CloseCurrentConnection();
        _listener?.Stop();
        _streamLock.Dispose();

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private void SetState(TransportState state)
    {
        _state = state;
        _stateChannel.Writer.TryWrite(state);
    }

    private void CloseCurrentConnection()
    {
        _stream?.Dispose();
        _stream = null;
        _tcpClient?.Dispose();
        _tcpClient = null;
    }

    private async Task ReconnectAsync(CancellationToken ct)
    {
        if (_options.IsServer)
        {
            if (_listener is null)
            {
                _listener = new TcpListener(IPAddress.Parse(_options.Host), _options.Port);
                _listener.Start();
            }
            _tcpClient = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
        }
        else
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_options.Host, _options.Port, ct).ConfigureAwait(false);
        }
        _stream = _tcpClient.GetStream();
        SetState(TransportState.Connected);

        // Flush buffered packets
        while (_reconnectBuffer.Reader.TryRead(out var buffered))
        {
            try
            {
                await _stream.WriteAsync(buffered, ct).ConfigureAwait(false);
            }
            catch
            {
                break;
            }
        }
    }

    private static byte[] WrapFrame(byte[] payload)
    {
        var frame = new byte[4 + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(frame, payload.Length);
        payload.CopyTo(frame, 4);
        return frame;
    }

    private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead), ct).ConfigureAwait(false);
            if (read == 0)
                return totalRead;
            totalRead += read;
        }
        return totalRead;
    }
}
