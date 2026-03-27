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

using System.Collections.Generic;
using ResQ.Mavlink.Protocol;

namespace ResQ.Mavlink.Transport;

/// <summary>
/// Stateful byte-stream framing parser for MAVLink v2 packets.
/// Accumulates bytes from a stream source (TCP, serial) and yields complete parsed packets.
/// Handles partial reads correctly by buffering across calls.
/// </summary>
public sealed class MavlinkFrameParser
{
    private readonly List<byte> _buffer = new(512);

    /// <summary>
    /// Feeds new bytes into the parser buffer.
    /// </summary>
    /// <param name="data">Bytes received from the underlying stream.</param>
    /// <param name="count">Number of valid bytes in <paramref name="data"/>.</param>
    public void Feed(byte[] data, int count)
    {
        for (var i = 0; i < count; i++)
            _buffer.Add(data[i]);
    }

    /// <summary>
    /// Attempts to extract all complete MAVLink packets currently in the buffer.
    /// Partial packets remain buffered for the next <see cref="Feed"/> call.
    /// </summary>
    /// <returns>A list of successfully parsed <see cref="MavlinkPacket"/> instances.</returns>
    public List<MavlinkPacket> TryExtract()
    {
        var packets = new List<MavlinkPacket>();

        while (_buffer.Count > 0)
        {
            // Scan for STX
            var stxIndex = -1;
            for (var i = 0; i < _buffer.Count; i++)
            {
                if (_buffer[i] == MavlinkConstants.StxV2)
                {
                    stxIndex = i;
                    break;
                }
            }

            if (stxIndex < 0)
            {
                // No STX found — discard everything
                _buffer.Clear();
                break;
            }

            // Discard bytes before STX
            if (stxIndex > 0)
                _buffer.RemoveRange(0, stxIndex);

            // Need at least the minimum packet length to determine expected size
            if (_buffer.Count < MavlinkConstants.MinPacketLength)
                break;

            var payloadLen = _buffer[1];
            var incompatFlags = _buffer[2];
            var isSigned = (incompatFlags & MavlinkConstants.IncompatFlagSigned) != 0;
            var expectedLen = 1 + MavlinkConstants.HeaderLength + payloadLen + MavlinkConstants.CrcLength;
            if (isSigned)
                expectedLen += MavlinkConstants.SignatureLength;

            if (_buffer.Count < expectedLen)
                break; // Not enough bytes yet

            // Copy out the candidate frame
            var frame = new byte[expectedLen];
            for (var i = 0; i < expectedLen; i++)
                frame[i] = _buffer[i];

            if (MavlinkCodec.TryParse(frame, out var packet) && packet is not null)
            {
                packets.Add(packet);
                _buffer.RemoveRange(0, expectedLen);
            }
            else
            {
                // Bad frame — skip past this STX and try again
                _buffer.RemoveAt(0);
            }
        }

        return packets;
    }

    /// <summary>
    /// Clears the internal buffer. Use when resetting after a reconnect.
    /// </summary>
    public void Reset() => _buffer.Clear();
}
