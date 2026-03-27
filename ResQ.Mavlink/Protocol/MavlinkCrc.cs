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

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ResQ.Mavlink.Protocol;

/// <summary>
/// CRC-16/MCRF4XX used by MAVLink for packet integrity, plus CRC extra seed lookup.
/// </summary>
public static class MavlinkCrc
{
    private const ushort InitialValue = 0xFFFF;

    // CRC extra seeds for common.xml messages (message_id → crc_extra).
    // Sourced from MAVLink common.xml message definitions. Thread-safe for dialect registration.
    private static readonly ConcurrentDictionary<uint, byte> CrcExtraTable = new(
        new Dictionary<uint, byte>()
    {
        [0] = 50,     // HEARTBEAT
        [1] = 124,    // SYS_STATUS
        [11] = 89,    // SET_MODE
        [20] = 214,   // PARAM_REQUEST_READ
        [22] = 220,   // PARAM_VALUE
        [23] = 168,   // PARAM_SET
        [24] = 24,    // GPS_RAW_INT
        [30] = 39,    // ATTITUDE
        [33] = 104,   // GLOBAL_POSITION_INT
        [40] = 230,   // MISSION_REQUEST
        [42] = 28,    // MISSION_CURRENT
        [44] = 221,   // MISSION_COUNT
        [47] = 153,   // MISSION_ACK
        [51] = 196,   // MISSION_REQUEST_INT
        [70] = 124,   // RC_CHANNELS_OVERRIDE
        [73] = 38,    // MISSION_ITEM_INT
        [74] = 20,    // VFR_HUD
        [76] = 152,   // COMMAND_LONG
        [77] = 143,   // COMMAND_ACK
        [86] = 5,     // SET_POSITION_TARGET_GLOBAL_INT
        [87] = 150,   // POSITION_TARGET_GLOBAL_INT
        [242] = 104,  // HOME_POSITION
        [245] = 130,  // EXTENDED_SYS_STATE
        [253] = 83,   // STATUSTEXT
    });

    /// <summary>
    /// Computes the CRC-16/MCRF4XX over <paramref name="data"/>.
    /// </summary>
    public static ushort Calculate(ReadOnlySpan<byte> data)
    {
        var crc = InitialValue;
        for (var i = 0; i < data.Length; i++)
            crc = Accumulate(crc, data[i]);
        return crc;
    }

    /// <summary>
    /// Feeds a single byte into an ongoing CRC calculation.
    /// </summary>
    public static ushort Accumulate(ushort crc, byte value)
    {
        var tmp = (byte)(value ^ (byte)(crc & 0xFF));
        tmp ^= (byte)(tmp << 4);
        return (ushort)((crc >> 8) ^ (tmp << 8) ^ (tmp << 3) ^ (tmp >> 4));
    }

    /// <summary>
    /// Returns the CRC extra seed for a given message ID, or null if unknown.
    /// </summary>
    public static byte? GetCrcExtra(uint messageId) =>
        CrcExtraTable.TryGetValue(messageId, out var extra) ? extra : null;

    /// <summary>
    /// Registers a CRC extra for a custom dialect message. Used by dialect extensions.
    /// </summary>
    public static void RegisterCrcExtra(uint messageId, byte crcExtra) =>
        CrcExtraTable[messageId] = crcExtra;
}
