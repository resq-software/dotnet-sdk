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
using ResQ.Mavlink.Messages;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Gateway.Translation;

/// <summary>
/// Provides bidirectional, stateless translation between MAVLink messages and ResQ domain types.
/// </summary>
/// <remarks>
/// All methods are pure functions — no side-effects, no shared mutable state.
/// Unit conversion constants follow the MAVLink specification:
/// <list type="bullet">
/// <item><description>Lat/Lon: degE7 (integer degrees × 1 × 10⁷) ↔ decimal degrees</description></item>
/// <item><description>Altitude: millimetres ↔ metres</description></item>
/// <item><description>Velocity: cm/s ↔ m/s</description></item>
/// <item><description>Battery voltage: millivolts ↔ volts</description></item>
/// </list>
/// </remarks>
public static class MessageTranslator
{
    private const double DegE7Scale = 1e7;
    private const double AltMmToM = 1e-3;
    private const double VelCmToM = 1e-2;

    /// <summary>
    /// Translates MAVLink telemetry messages into a single ResQ <see cref="TelemetryPacket"/>.
    /// </summary>
    /// <param name="droneId">The ResQ drone identifier string.</param>
    /// <param name="pos">
    /// The <see cref="GlobalPositionInt"/> message providing position and velocity data.
    /// </param>
    /// <param name="att">
    /// Optional <see cref="Attitude"/> message. Not currently mapped to <see cref="TelemetryPacket"/> fields
    /// but reserved for future attitude-derived status.
    /// </param>
    /// <param name="sys">
    /// Optional <see cref="SysStatus"/> message providing battery and sensor health data.
    /// </param>
    /// <param name="hb">
    /// Optional <see cref="Heartbeat"/> message providing armed state and system status.
    /// </param>
    /// <returns>A fully populated <see cref="TelemetryPacket"/>.</returns>
    public static TelemetryPacket MapToTelemetry(
        string droneId,
        GlobalPositionInt pos,
        Attitude? att,
        SysStatus? sys,
        Heartbeat? hb)
    {
        var lat = pos.Lat / DegE7Scale;
        var lon = pos.Lon / DegE7Scale;
        var altM = pos.Alt * AltMmToM;

        var vx = pos.Vx * VelCmToM;
        var vy = pos.Vy * VelCmToM;
        var vz = pos.Vz * VelCmToM;

        float batteryPercent = 0f;
        float batteryVoltage = 0f;
        if (sys.HasValue)
        {
            var remaining = sys.Value.BatteryRemaining;
            batteryPercent = remaining < 0 ? 0f : (float)remaining;
            batteryVoltage = sys.Value.VoltageBattery / 1000f;
        }

        var status = DroneStatus.Idle;
        if (hb.HasValue)
        {
            status = MavStateMapper.MapDroneStatus(hb.Value.SystemStatus);
        }

        return new TelemetryPacket
        {
            DroneId = droneId,
            Timestamp = DateTimeOffset.UtcNow,
            Position = new Location(lat, lon, altM),
            Velocity = new Velocity(vx, vy, vz),
            Status = status,
            BatteryPercent = batteryPercent,
            BatteryVoltage = batteryVoltage,
        };
    }

    /// <summary>
    /// Creates a MAVLink <see cref="SetPositionTargetGlobalInt"/> message targeting the given geographic position.
    /// </summary>
    /// <param name="latDeg">Target latitude in decimal degrees.</param>
    /// <param name="lonDeg">Target longitude in decimal degrees.</param>
    /// <param name="altMetres">Target altitude in metres above mean sea level.</param>
    /// <returns>
    /// A <see cref="SetPositionTargetGlobalInt"/> with position-only type mask
    /// (velocities and accelerations ignored), using the global int coordinate frame.
    /// </returns>
    /// <remarks>
    /// The type mask value <c>0b111111111000</c> (0xFF8 = 4088) instructs the vehicle
    /// to use only the position fields and ignore velocity/acceleration/yaw setpoints.
    /// </remarks>
    public static SetPositionTargetGlobalInt MapToSetPositionTarget(
        double latDeg,
        double lonDeg,
        float altMetres)
    {
        // TypeMask: ignore velocity (bits 3-5), acceleration (bits 6-8), yaw (bit 10), yaw-rate (bit 11)
        // Leave position bits (0-2) unset (= use position).
        const ushort typeMaskPositionOnly = 0b_1111_1111_1000;

        return new SetPositionTargetGlobalInt
        {
            LatInt = (int)Math.Round(latDeg * DegE7Scale),
            LonInt = (int)Math.Round(lonDeg * DegE7Scale),
            Alt = altMetres,
            TypeMask = typeMaskPositionOnly,
            CoordinateFrame = MavFrame.GlobalInt,
        };
    }

    /// <summary>
    /// Maps a ResQ <see cref="FlightCommandType"/> to the equivalent MAVLink <see cref="CommandLong"/> message.
    /// </summary>
    /// <param name="type">The ResQ flight command type to translate.</param>
    /// <returns>
    /// A <see cref="CommandLong"/> configured with the appropriate <see cref="MavCmd"/> identifier.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="type"/> is not a recognised <see cref="FlightCommandType"/> value.
    /// </exception>
    public static CommandLong MapFlightCommandToMavlink(FlightCommandType type)
    {
        var cmd = type switch
        {
            FlightCommandType.Hover => MavCmd.NavLoiterUnlim,
            FlightCommandType.GoToWaypoint => MavCmd.NavWaypoint,
            FlightCommandType.ReturnToLaunch => MavCmd.NavReturnToLaunch,
            FlightCommandType.Land => MavCmd.NavLand,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown FlightCommandType"),
        };

        return new CommandLong { Command = cmd };
    }
}
