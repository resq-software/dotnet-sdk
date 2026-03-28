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

using ResQ.Mavlink.Protocol;

namespace ResQ.Mavlink.Transport;

/// <summary>
/// Abstraction over a bidirectional MAVLink packet transport (UDP, TCP, serial, etc.).
/// </summary>
public interface IMavlinkTransport : IAsyncDisposable
{
    /// <summary>Gets the current connection state of the transport.</summary>
    TransportState State { get; }

    /// <summary>
    /// Serializes and sends a <see cref="MavlinkPacket"/> over the transport.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask SendAsync(MavlinkPacket packet, CancellationToken ct = default);

    /// <summary>
    /// Returns an async sequence of received and parsed <see cref="MavlinkPacket"/> instances.
    /// Completes when <paramref name="ct"/> is cancelled or the transport is disposed.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<MavlinkPacket> ReceiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns an async sequence of <see cref="TransportState"/> transitions.
    /// Always yields <see cref="TransportState.Connected"/> on subscription start,
    /// and <see cref="TransportState.Disposed"/> when disposed.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<TransportState> StateChanges(CancellationToken ct = default);
}
