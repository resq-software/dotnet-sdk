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

namespace ResQ.Mavlink.Gateway.State;

/// <summary>
/// Mutable snapshot of a MAVLink vehicle's telemetry state, updated in-place as messages arrive.
/// </summary>
/// <remarks>
/// An instance of <see cref="VehicleState"/> is maintained per <see cref="SystemId"/> inside
/// <see cref="VehicleStateTracker"/>. Individual fields are updated by whichever MAVLink message
/// carries the relevant data; all updates also refresh <see cref="LastSeen"/>.
/// </remarks>
public sealed class VehicleState
{
    /// <summary>MAVLink system ID that uniquely identifies this vehicle on the link.</summary>
    public byte SystemId { get; init; }

    /// <summary>Latitude in decimal degrees (WGS-84).</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude in decimal degrees (WGS-84).</summary>
    public double Longitude { get; set; }

    /// <summary>Altitude above mean sea level, in metres.</summary>
    public double AltitudeMetres { get; set; }

    /// <summary>Altitude above home position (relative altitude), in metres.</summary>
    public double RelativeAltMetres { get; set; }

    /// <summary>Roll angle in radians (-π … +π).</summary>
    public float Roll { get; set; }

    /// <summary>Pitch angle in radians (-π … +π).</summary>
    public float Pitch { get; set; }

    /// <summary>Yaw angle in radians (-π … +π).</summary>
    public float Yaw { get; set; }

    /// <summary>Remaining battery charge as a percentage (0–100).</summary>
    public double BatteryPercent { get; set; }

    /// <summary>Battery terminal voltage in volts.</summary>
    public double BatteryVoltage { get; set; }

    /// <summary>Human-readable vehicle status string, derived from the MAVLink system state.</summary>
    public string Status { get; set; } = "IDLE";

    /// <summary><see langword="true"/> when the vehicle's motors are armed.</summary>
    public bool IsArmed { get; set; }

    /// <summary>Sequence number of the waypoint currently being executed.</summary>
    public ushort CurrentWaypoint { get; set; }

    /// <summary>UTC timestamp of the most recent message received from this vehicle.</summary>
    public DateTimeOffset LastSeen { get; set; }
}
