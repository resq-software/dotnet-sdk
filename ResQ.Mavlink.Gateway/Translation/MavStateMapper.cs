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

using ResQ.Core;
using ResQ.Mavlink.Enums;

namespace ResQ.Mavlink.Gateway.Translation;

/// <summary>
/// Provides pure, stateless mapping functions between MAVLink state enums and ResQ domain types.
/// </summary>
/// <remarks>
/// All methods are thread-safe by design — no shared state is mutated.
/// </remarks>
public static class MavStateMapper
{
    /// <summary>
    /// Maps a MAVLink <see cref="MavState"/> to the corresponding ResQ <see cref="DroneStatus"/>.
    /// </summary>
    /// <param name="state">The MAVLink system state reported in a HEARTBEAT message.</param>
    /// <returns>The ResQ <see cref="DroneStatus"/> that best represents the given MAVLink state.</returns>
    /// <remarks>
    /// Mapping rules:
    /// <list type="bullet">
    /// <item><description><see cref="MavState.Uninit"/>, <see cref="MavState.Boot"/>, <see cref="MavState.Calibrating"/>, <see cref="MavState.Standby"/> → <see cref="DroneStatus.Idle"/></description></item>
    /// <item><description><see cref="MavState.Active"/> → <see cref="DroneStatus.InFlight"/></description></item>
    /// <item><description><see cref="MavState.Critical"/>, <see cref="MavState.Emergency"/> → <see cref="DroneStatus.Emergency"/></description></item>
    /// <item><description><see cref="MavState.Poweroff"/>, <see cref="MavState.FlightTermination"/> → <see cref="DroneStatus.Offline"/></description></item>
    /// </list>
    /// </remarks>
    public static DroneStatus MapDroneStatus(MavState state)
    {
        return state switch
        {
            MavState.Uninit or MavState.Boot or MavState.Calibrating or MavState.Standby => DroneStatus.Idle,
            MavState.Active => DroneStatus.InFlight,
            MavState.Critical or MavState.Emergency => DroneStatus.Emergency,
            MavState.Poweroff or MavState.FlightTermination => DroneStatus.Offline,
            _ => DroneStatus.Idle,
        };
    }

    /// <summary>
    /// Determines whether the GPS fix type indicates an acceptable position lock.
    /// </summary>
    /// <param name="fix">The GPS fix type reported in a GPS_RAW_INT or similar message.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="fix"/> is <see cref="GpsFixType.Fix3d"/> or better
    /// (i.e., DGPS, RTK float, RTK fixed, static, or PPP); <see langword="false"/> otherwise.
    /// </returns>
    public static bool MapGpsOk(GpsFixType fix)
    {
        return fix >= GpsFixType.Fix3d;
    }

    /// <summary>
    /// Determines whether the drone is armed based on the MAVLink base-mode flags.
    /// </summary>
    /// <param name="baseMode">The base-mode bitmask from a HEARTBEAT message.</param>
    /// <returns>
    /// <see langword="true"/> when the <see cref="MavModeFlag.SafetyArmed"/> bit is set;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public static bool MapIsArmed(MavModeFlag baseMode)
    {
        return (baseMode & MavModeFlag.SafetyArmed) != 0;
    }
}
