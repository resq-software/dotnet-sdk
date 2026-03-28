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
/// MAVLink v2 protocol constants.
/// </summary>
public static class MavlinkConstants
{
    /// <summary>MAVLink v2 start-of-frame marker.</summary>
    public const byte StxV2 = 0xFD;

    /// <summary>MAVLink v1 start-of-frame marker (for detection/rejection).</summary>
    public const byte StxV1 = 0xFE;

    /// <summary>MAVLink v2 header length in bytes (excluding STX).</summary>
    public const int HeaderLength = 9;

    /// <summary>CRC length in bytes (CRC-16 = 2 bytes).</summary>
    public const int CrcLength = 2;

    /// <summary>Signature length when present (link ID + timestamp + signature bytes).</summary>
    public const int SignatureLength = 13;

    /// <summary>Maximum payload length for MAVLink v2.</summary>
    public const int MaxPayloadLength = 255;

    /// <summary>Minimum packet size: STX + header + CRC (no payload).</summary>
    public const int MinPacketLength = 1 + HeaderLength + CrcLength;

    /// <summary>Incompatibility flag indicating the packet is signed.</summary>
    public const byte IncompatFlagSigned = 0x01;
}
