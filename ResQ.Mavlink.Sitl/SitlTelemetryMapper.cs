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

using System.Numerics;
using ResQ.Mavlink.Messages;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl;

/// <summary>
/// Maps MAVLink telemetry messages (<see cref="GlobalPositionInt"/> and <see cref="Attitude"/>)
/// to a <see cref="DronePhysicsState"/> using equirectangular coordinate approximation and
/// NED-to-ENU axis conversion.
/// </summary>
public static class SitlTelemetryMapper
{
    /// <summary>
    /// Metres per degree of latitude (approximate, valid for small displacements).
    /// </summary>
    private const double MetresPerDegreeLat = 111_319.49;

    /// <summary>
    /// Earth radius in metres used for longitude scaling.
    /// </summary>
    private const double EarthRadiusMetres = 6_371_000.0;

    /// <summary>
    /// Maps a <see cref="GlobalPositionInt"/> MAVLink message and an <see cref="Attitude"/> MAVLink
    /// message to a <see cref="DronePhysicsState"/>.
    /// </summary>
    /// <param name="position">The GLOBAL_POSITION_INT telemetry message.</param>
    /// <param name="attitude">The ATTITUDE telemetry message.</param>
    /// <param name="batteryPercent">
    /// Optional battery charge percentage [0, 100]. Defaults to <c>100</c> when not provided.
    /// </param>
    /// <returns>The mapped <see cref="DronePhysicsState"/>.</returns>
    public static DronePhysicsState Map(
        GlobalPositionInt position,
        Attitude attitude,
        double batteryPercent = 100.0)
    {
        // --- Position ---
        // lat/lon are in degE7 (integer degrees * 1e7).
        // Convert to world-space metres using equirectangular approximation.
        // Coordinate system: X = East, Y = Up, Z = South (left-handed ENU variant).
        double latDeg = position.Lat / 1e7;
        double lonDeg = position.Lon / 1e7;

        // X = East offset in metres.
        double cosLat = Math.Cos(latDeg * Math.PI / 180.0);
        float x = (float)(lonDeg * MetresPerDegreeLat * cosLat);

        // Y = Up (altitude above ground, RelativeAlt is in mm).
        float y = position.RelativeAlt / 1000.0f;

        // Z = South (negate latitude offset for South-positive Z axis).
        float z = -(float)(latDeg * MetresPerDegreeLat);

        var worldPosition = new Vector3(x, y, z);

        // --- Velocity ---
        // MAVLink NED velocities: Vx = North, Vy = East, Vz = Down (cm/s).
        // ENU mapping: X = East = Vy, Y = Up = -Vz, Z = South = -Vx.
        float vx = position.Vy / 100.0f;   // East
        float vy = -position.Vz / 100.0f;  // Up (negate Down)
        float vz = -position.Vx / 100.0f;  // South (negate North)

        var velocity = new Vector3(vx, vy, vz);

        // --- Orientation ---
        // MAVLink ATTITUDE gives roll/pitch/yaw in radians (NED body frame).
        // System.Numerics.Quaternion.CreateFromYawPitchRoll uses Z-Y-X Euler (yaw=Y, pitch=X, roll=Z)
        // which maps directly to MAVLink's yaw/pitch/roll convention.
        var orientation = Quaternion.CreateFromYawPitchRoll(attitude.Yaw, attitude.Pitch, attitude.Roll);

        // --- Angular Velocity ---
        // NED body rates: rollspeed, pitchspeed, yawspeed (rad/s).
        // Map to ENU: X = East ≈ rollspeed, Y = Up ≈ yawspeed, Z = South ≈ pitchspeed.
        var angularVelocity = new Vector3(
            attitude.Rollspeed,
            attitude.Yawspeed,
            attitude.Pitchspeed);

        return new DronePhysicsState(
            worldPosition,
            velocity,
            orientation,
            angularVelocity,
            batteryPercent);
    }
}
