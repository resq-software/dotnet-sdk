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
/// An immutable representation of a parsed MAVLink v2 packet.
/// </summary>
/// <param name="sequenceNumber">Packet sequence number (0-255, wrapping).</param>
/// <param name="systemId">Sending system ID (1-255).</param>
/// <param name="componentId">Sending component ID (1-255).</param>
/// <param name="messageId">24-bit message ID.</param>
/// <param name="payload">Raw message payload bytes (zero-copy via Memory).</param>
/// <param name="incompatFlags">Incompatibility flags.</param>
/// <param name="compatFlags">Compatibility flags.</param>
/// <param name="signature">Optional 13-byte signature (null if unsigned).</param>
public sealed record MavlinkPacket(
    byte sequenceNumber,
    byte systemId,
    byte componentId,
    uint messageId,
    ReadOnlyMemory<byte> payload,
    byte incompatFlags,
    byte compatFlags,
    ReadOnlyMemory<byte>? signature)
{
    /// <summary>Gets the packet sequence number (0-255, wrapping).</summary>
    public byte SequenceNumber { get; } = sequenceNumber;

    /// <summary>Gets the sending system ID (1-255).</summary>
    public byte SystemId { get; } = systemId;

    /// <summary>Gets the sending component ID (1-255).</summary>
    public byte ComponentId { get; } = componentId;

    /// <summary>Gets the 24-bit message ID.</summary>
    public uint MessageId { get; } = messageId;

    /// <summary>Gets the raw message payload bytes.</summary>
    public ReadOnlyMemory<byte> Payload { get; } = payload;

    /// <summary>Gets the incompatibility flags.</summary>
    public byte IncompatFlags { get; } = incompatFlags;

    /// <summary>Gets the compatibility flags.</summary>
    public byte CompatFlags { get; } = compatFlags;

    /// <summary>Gets the optional 13-byte signature (null if unsigned).</summary>
    public ReadOnlyMemory<byte>? Signature { get; } = signature;

    /// <summary>Gets the payload length.</summary>
    public int PayloadLength => Payload.Length;

    /// <summary>Gets whether this packet has a signature.</summary>
    public bool IsSigned => (IncompatFlags & MavlinkConstants.IncompatFlagSigned) != 0
                            && Signature is not null;
}
