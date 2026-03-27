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
            [2] = 137,    // SYSTEM_TIME
            [4] = 237,    // PING
            [11] = 89,    // SET_MODE
            [20] = 214,   // PARAM_REQUEST_READ
            [22] = 220,   // PARAM_VALUE
            [23] = 168,   // PARAM_SET
            [24] = 24,    // GPS_RAW_INT
            [26] = 170,   // SCALED_IMU
            [27] = 144,   // RAW_IMU
            [29] = 115,   // SCALED_PRESSURE
            [30] = 39,    // ATTITUDE
            [32] = 185,   // LOCAL_POSITION_NED
            [33] = 104,   // GLOBAL_POSITION_INT
            [36] = 222,   // SERVO_OUTPUT_RAW
            [40] = 230,   // MISSION_REQUEST
            [41] = 28,    // MISSION_SET_CURRENT
            [42] = 28,    // MISSION_CURRENT
            [43] = 132,   // MISSION_REQUEST_LIST
            [44] = 221,   // MISSION_COUNT
            [45] = 232,   // MISSION_CLEAR_ALL
            [47] = 153,   // MISSION_ACK
            [49] = 39,    // GPS_GLOBAL_ORIGIN
            [51] = 196,   // MISSION_REQUEST_INT
            [61] = 246,   // ATTITUDE_QUATERNION
            [62] = 183,   // NAV_CONTROLLER_OUTPUT
            [63] = 119,   // GLOBAL_POSITION_INT_COV
            [70] = 124,   // RC_CHANNELS_OVERRIDE
            [73] = 38,    // MISSION_ITEM_INT
            [74] = 20,    // VFR_HUD
            [76] = 152,   // COMMAND_LONG
            [77] = 143,   // COMMAND_ACK
            [83] = 22,    // ATTITUDE_TARGET
            [84] = 143,   // SET_POSITION_TARGET_LOCAL_NED
            [85] = 140,   // POSITION_TARGET_LOCAL_NED
            [86] = 5,     // SET_POSITION_TARGET_GLOBAL_INT
            [87] = 150,   // POSITION_TARGET_GLOBAL_INT
            [105] = 93,   // HIGHRES_IMU
            [109] = 185,  // RADIO_STATUS
            [111] = 34,   // TIMESYNC
            [116] = 76,   // SCALED_IMU2
            [125] = 203,  // POWER_STATUS
            [133] = 6,    // TERRAIN_REQUEST
            [134] = 229,  // TERRAIN_DATA
            [135] = 203,  // TERRAIN_CHECK
            [136] = 1,    // TERRAIN_REPORT
            [140] = 181,  // ACTUATOR_CONTROL_TARGET
            [147] = 154,  // BATTERY_STATUS
            [148] = 178,  // AUTOPILOT_VERSION
            [230] = 163,  // ESTIMATOR_STATUS
            [231] = 105,  // WIND_COV
            [233] = 35,   // GPS_RTCM_DATA
            [241] = 90,   // VIBRATION
            [242] = 104,  // HOME_POSITION
            [245] = 130,  // EXTENDED_SYS_STATE
            [253] = 83,   // STATUSTEXT
            [263] = 133,  // CAMERA_IMAGE_CAPTURED
            [265] = 26,   // MOUNT_ORIENTATION
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
