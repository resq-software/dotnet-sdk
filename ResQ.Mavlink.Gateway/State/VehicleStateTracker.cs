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
using ResQ.Core;
using ResQ.Mavlink.Gateway.Translation;
using ResQ.Mavlink.Messages;

namespace ResQ.Mavlink.Gateway.State;

/// <summary>
/// Thread-safe tracker that maintains a <see cref="VehicleState"/> per MAVLink system ID,
/// updated as incoming messages are processed.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Update"/> is the primary ingestion point. Call it for every decoded
/// <see cref="IMavlinkMessage"/> and the tracker will dispatch to the appropriate
/// field-update logic based on the concrete message type.
/// </para>
/// <para>
/// Unit conversions applied during update:
/// <list type="bullet">
/// <item><description>Lat/Lon: degE7 (integer × 10⁷) → decimal degrees (÷ 1e7)</description></item>
/// <item><description>Altitude: millimetres → metres (÷ 1000)</description></item>
/// <item><description>Battery voltage: millivolts → volts (÷ 1000)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class VehicleStateTracker
{
    private const double DegE7Scale = 1e7;
    private const double AltMmToM = 1e-3;

    private readonly ConcurrentDictionary<byte, VehicleState> _vehicles = new();

    /// <summary>
    /// Updates the tracked state for the given <paramref name="systemId"/> based on the
    /// content of <paramref name="message"/>.
    /// </summary>
    /// <param name="systemId">The MAVLink system ID of the sending vehicle.</param>
    /// <param name="message">The decoded MAVLink message to apply.</param>
    /// <remarks>
    /// Recognised message types:
    /// <list type="bullet">
    /// <item><description><see cref="GlobalPositionInt"/> — updates lat/lon/alt/relative-alt.</description></item>
    /// <item><description><see cref="Attitude"/> — updates roll/pitch/yaw.</description></item>
    /// <item><description><see cref="SysStatus"/> — updates battery percent and voltage.</description></item>
    /// <item><description><see cref="Heartbeat"/> — updates status string and armed flag via <see cref="MavStateMapper"/>.</description></item>
    /// <item><description><see cref="MissionCurrent"/> — updates the current waypoint sequence.</description></item>
    /// </list>
    /// All updates also refresh <see cref="VehicleState.LastSeen"/>.
    /// </remarks>
    public void Update(byte systemId, IMavlinkMessage message)
    {
        var state = _vehicles.GetOrAdd(systemId, id => new VehicleState { SystemId = id });

        switch (message)
        {
            case GlobalPositionInt pos:
                state.Latitude = pos.Lat / DegE7Scale;
                state.Longitude = pos.Lon / DegE7Scale;
                state.AltitudeMetres = pos.Alt * AltMmToM;
                state.RelativeAltMetres = pos.RelativeAlt * AltMmToM;
                break;

            case Attitude att:
                state.Roll = att.Roll;
                state.Pitch = att.Pitch;
                state.Yaw = att.Yaw;
                break;

            case SysStatus sys:
                state.BatteryPercent = sys.BatteryRemaining < 0 ? 0.0 : (double)sys.BatteryRemaining;
                state.BatteryVoltage = sys.VoltageBattery / 1000.0;
                break;

            case Heartbeat hb:
                state.Status = MavStateMapper.MapDroneStatus(hb.SystemStatus).ToString().ToUpperInvariant();
                state.IsArmed = MavStateMapper.MapIsArmed(hb.BaseMode);
                break;

            case MissionCurrent mc:
                state.CurrentWaypoint = mc.Seq;
                break;
        }

        state.LastSeen = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Returns the <see cref="VehicleState"/> for the given <paramref name="systemId"/>,
    /// or <see langword="null"/> if the vehicle has not yet been seen.
    /// </summary>
    /// <param name="systemId">The MAVLink system ID to look up.</param>
    /// <returns>The tracked state, or <see langword="null"/>.</returns>
    public VehicleState? GetVehicle(byte systemId)
    {
        _vehicles.TryGetValue(systemId, out var state);
        return state;
    }

    /// <summary>
    /// Returns a read-only snapshot of all currently tracked vehicles.
    /// </summary>
    /// <returns>All <see cref="VehicleState"/> instances, in no guaranteed order.</returns>
    public IReadOnlyCollection<VehicleState> GetAllVehicles()
    {
        return _vehicles.Values.ToArray();
    }

    /// <summary>
    /// Builds a <see cref="TelemetryPacket"/> from the current state of the specified vehicle.
    /// </summary>
    /// <param name="systemId">The MAVLink system ID of the vehicle to snapshot.</param>
    /// <returns>
    /// A <see cref="TelemetryPacket"/> populated from the latest cached fields,
    /// or <see langword="null"/> if the vehicle has not yet been seen.
    /// </returns>
    /// <remarks>
    /// The <see cref="TelemetryPacket.DroneId"/> is set to <c>"mavlink-{systemId}"</c>.
    /// Field mapping follows the same conventions as <see cref="MessageTranslator.MapToTelemetry"/>.
    /// </remarks>
    public TelemetryPacket? ToTelemetryPacket(byte systemId)
    {
        if (!_vehicles.TryGetValue(systemId, out var state))
        {
            return null;
        }

        return new TelemetryPacket
        {
            DroneId = $"mavlink-{systemId}",
            Timestamp = state.LastSeen,
            Position = new Location(state.Latitude, state.Longitude, state.AltitudeMetres),
            BatteryPercent = (float)state.BatteryPercent,
            BatteryVoltage = (float)state.BatteryVoltage,
            Status = Enum.TryParse<DroneStatus>(state.Status, ignoreCase: true, out var ds)
                ? ds
                : DroneStatus.Idle,
        };
    }
}
