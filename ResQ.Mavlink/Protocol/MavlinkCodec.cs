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

namespace ResQ.Mavlink.Protocol;

/// <summary>
/// Stateless MAVLink v2 packet serializer and parser.
/// </summary>
public static class MavlinkCodec
{
    /// <summary>
    /// Serializes a <see cref="MavlinkPacket"/> into a byte array with proper framing and CRC.
    /// </summary>
    /// <param name="packet">The packet to serialize.</param>
    /// <returns>The framed wire bytes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CRC extra is not known for the message ID.</exception>
    public static byte[] Serialize(MavlinkPacket packet)
    {
        var crcExtra = MavlinkCrc.GetCrcExtra(packet.MessageId)
            ?? throw new InvalidOperationException(
                $"No CRC extra registered for message ID {packet.MessageId}. Register via MavlinkCrc.RegisterCrcExtra().");

        var payloadLen = packet.Payload.Length;
        var totalLen = 1 + MavlinkConstants.HeaderLength + payloadLen + MavlinkConstants.CrcLength;
        if (packet.IsSigned)
            totalLen += MavlinkConstants.SignatureLength;

        var buffer = new byte[totalLen];
        var offset = 0;

        // STX
        buffer[offset++] = MavlinkConstants.StxV2;

        // Header (9 bytes)
        buffer[offset++] = (byte)payloadLen;
        buffer[offset++] = packet.IncompatFlags;
        buffer[offset++] = packet.CompatFlags;
        buffer[offset++] = packet.SequenceNumber;
        buffer[offset++] = packet.SystemId;
        buffer[offset++] = packet.ComponentId;
        buffer[offset++] = (byte)(packet.MessageId & 0xFF);
        buffer[offset++] = (byte)((packet.MessageId >> 8) & 0xFF);
        buffer[offset++] = (byte)((packet.MessageId >> 16) & 0xFF);

        // Payload
        packet.Payload.Span.CopyTo(buffer.AsSpan(offset, payloadLen));
        offset += payloadLen;

        // CRC over header (bytes 1..9) + payload + crc_extra
        var crcSpan = buffer.AsSpan(1, MavlinkConstants.HeaderLength + payloadLen);
        var crc = MavlinkCrc.Calculate(crcSpan);
        crc = MavlinkCrc.Accumulate(crc, crcExtra);

        buffer[offset++] = (byte)(crc & 0xFF);
        buffer[offset++] = (byte)(crc >> 8);

        // Signature (if present)
        if (packet.IsSigned && packet.Signature is not null)
        {
            packet.Signature.Value.Span.CopyTo(buffer.AsSpan(offset, MavlinkConstants.SignatureLength));
        }

        return buffer;
    }

    /// <summary>
    /// Attempts to parse a MAVLink v2 packet from <paramref name="data"/>.
    /// </summary>
    /// <param name="data">Raw bytes starting with STX.</param>
    /// <param name="packet">The parsed packet, or null on failure.</param>
    /// <returns><c>true</c> if parsing succeeded; <c>false</c> otherwise.</returns>
    public static bool TryParse(ReadOnlySpan<byte> data, out MavlinkPacket? packet)
    {
        packet = null;

        if (data.Length < MavlinkConstants.MinPacketLength)
            return false;

        if (data[0] != MavlinkConstants.StxV2)
            return false;

        var payloadLen = data[1];
        var incompatFlags = data[2];
        var compatFlags = data[3];
        var seq = data[4];
        var sysId = data[5];
        var compId = data[6];
        var msgId = (uint)(data[7] | (data[8] << 8) | (data[9] << 16));

        var expectedLen = 1 + MavlinkConstants.HeaderLength + payloadLen + MavlinkConstants.CrcLength;
        var isSigned = (incompatFlags & MavlinkConstants.IncompatFlagSigned) != 0;
        if (isSigned)
            expectedLen += MavlinkConstants.SignatureLength;

        if (data.Length < expectedLen)
            return false;

        // Verify CRC
        var crcExtra = MavlinkCrc.GetCrcExtra(msgId);
        if (crcExtra is null)
            return false; // Unknown message — can't verify CRC

        var crcSpan = data.Slice(1, MavlinkConstants.HeaderLength + payloadLen);
        var crc = MavlinkCrc.Calculate(crcSpan);
        crc = MavlinkCrc.Accumulate(crc, crcExtra.Value);

        var crcOffset = 1 + MavlinkConstants.HeaderLength + payloadLen;
        var wireCrc = (ushort)(data[crcOffset] | (data[crcOffset + 1] << 8));

        if (crc != wireCrc)
            return false;

        // Extract payload
        var payload = data.Slice(1 + MavlinkConstants.HeaderLength, payloadLen).ToArray();

        // Extract signature if present
        byte[]? signature = null;
        if (isSigned)
        {
            signature = data.Slice(crcOffset + MavlinkConstants.CrcLength, MavlinkConstants.SignatureLength).ToArray();
        }

        packet = new MavlinkPacket(seq, sysId, compId, msgId, payload, incompatFlags, compatFlags, signature);
        return true;
    }
}
