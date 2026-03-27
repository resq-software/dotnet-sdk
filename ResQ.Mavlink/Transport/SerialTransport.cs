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

using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Protocol;

namespace ResQ.Mavlink.Transport;

/// <summary>
/// Serial port <see cref="IMavlinkTransport"/> that wraps <see cref="SerialPort"/>.
/// Uses <see cref="MavlinkFrameParser"/> to handle partial reads correctly.
/// </summary>
public sealed class SerialTransport : IMavlinkTransport
{
    private readonly SerialTransportOptions _options;
    private readonly SerialPort _port;
    private readonly Channel<TransportState> _stateChannel;
    private TransportState _state;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="SerialTransport"/> using <see cref="IOptions{T}"/> for DI.
    /// </summary>
    /// <param name="options">Transport options wrapped in IOptions.</param>
    public SerialTransport(IOptions<SerialTransportOptions> options)
        : this(options.Value)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SerialTransport"/> with raw options.
    /// </summary>
    /// <param name="options">Transport options.</param>
    public SerialTransport(SerialTransportOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _stateChannel = Channel.CreateUnbounded<TransportState>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });

        _port = new SerialPort(options.PortName, options.BaudRate)
        {
            ReadTimeout = (int)options.ReadTimeout.TotalMilliseconds,
        };

        _state = TransportState.Connecting;
    }

    /// <inheritdoc/>
    public TransportState State => _state;

    /// <summary>
    /// Opens the serial port and transitions to <see cref="TransportState.Connected"/>.
    /// </summary>
    public void Open()
    {
        _port.Open();
        SetState(TransportState.Connected);
    }

    /// <inheritdoc/>
    public ValueTask SendAsync(MavlinkPacket packet, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var data = MavlinkCodec.Serialize(packet);
        _port.BaseStream.Write(data, 0, data.Length);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<MavlinkPacket> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var parser = new MavlinkFrameParser();
        var readBuf = new byte[256];

        while (!ct.IsCancellationRequested && !_disposed)
        {
            int count;
            try
            {
                count = await _port.BaseStream.ReadAsync(readBuf.AsMemory(0, readBuf.Length), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (Exception)
            {
                yield break;
            }

            if (count > 0)
            {
                parser.Feed(readBuf, count);
                foreach (var packet in parser.TryExtract())
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
        SetState(TransportState.Disposed);
        _stateChannel.Writer.Complete();

        if (_port.IsOpen)
            _port.Close();
        _port.Dispose();

        return ValueTask.CompletedTask;
    }

    private void SetState(TransportState state)
    {
        _state = state;
        _stateChannel.Writer.TryWrite(state);
    }
}
