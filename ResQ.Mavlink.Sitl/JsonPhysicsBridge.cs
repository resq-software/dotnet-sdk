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

using System.Globalization;
using System.Text;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl;

/// <summary>
/// Builds JSON payloads conforming to ArduPilot's <c>--model json</c> physics interface.
/// </summary>
/// <remarks>
/// See ArduPilot documentation for the JSON sensor format:
/// https://ardupilot.org/dev/docs/sitl-with-json.html
/// </remarks>
public static class JsonPhysicsBridge
{
    /// <summary>Standard gravity constant in m/s² (downward).</summary>
    private const double Gravity = 9.80665;

    /// <summary>
    /// Builds the sensor JSON string that ArduPilot expects from the physics backend
    /// during each simulation step.
    /// </summary>
    /// <param name="state">The current drone physics state.</param>
    /// <param name="timestampMicros">Elapsed simulation time in microseconds.</param>
    /// <returns>A JSON string suitable for sending to the ArduPilot SITL JSON socket.</returns>
    public static string BuildSensorJson(DronePhysicsState state, long timestampMicros)
    {
        // Extract body-frame acceleration: at rest, Z body axis should be approximately +9.81
        // (ArduPilot body-frame convention: Z-down body, so gravity appears as +g on Z).
        // For a hovering drone the accelerometer reads gravity opposite to the direction of free-fall.
        // In our ENU world, Y is Up; body Z-down maps to body accel_z ≈ +9.81 when stationary.
        double accelX = 0.0;
        double accelY = 0.0;
        double accelZ = Gravity; // gravity component along body Z-down axis

        // If the drone is moving, propagate velocity derivative here (simplified: zero for now).
        // A full implementation would track previous velocity and compute dv/dt.

        var sb = new StringBuilder(256);
        sb.Append('{');

        // Timestamp
        sb.Append("\"timestamp\":");
        sb.Append(timestampMicros.ToString(CultureInfo.InvariantCulture));

        // IMU gyro (body-frame angular velocity, rad/s)
        sb.Append(",\"imu\":{");
        sb.Append("\"gyro\":[");
        sb.Append(F(state.AngularVelocity.X));
        sb.Append(',');
        sb.Append(F(state.AngularVelocity.Y));
        sb.Append(',');
        sb.Append(F(state.AngularVelocity.Z));
        sb.Append(']');

        // IMU accelerometer body-frame (m/s²)
        sb.Append(",\"accel_body\":[");
        sb.Append(F(accelX));
        sb.Append(',');
        sb.Append(F(accelY));
        sb.Append(',');
        sb.Append(F(accelZ));
        sb.Append(']');
        sb.Append('}'); // end imu

        // Position (world-space metres, ENU: X=East, Y=Up, Z=South)
        sb.Append(",\"position\":[");
        sb.Append(F(state.Position.X));
        sb.Append(',');
        sb.Append(F(state.Position.Y));
        sb.Append(',');
        sb.Append(F(state.Position.Z));
        sb.Append(']');

        // Velocity (world-space m/s)
        sb.Append(",\"velocity\":[");
        sb.Append(F(state.Velocity.X));
        sb.Append(',');
        sb.Append(F(state.Velocity.Y));
        sb.Append(',');
        sb.Append(F(state.Velocity.Z));
        sb.Append(']');

        // Quaternion orientation [W, X, Y, Z]
        sb.Append(",\"quaternion\":[");
        sb.Append(F(state.Orientation.W));
        sb.Append(',');
        sb.Append(F(state.Orientation.X));
        sb.Append(',');
        sb.Append(F(state.Orientation.Y));
        sb.Append(',');
        sb.Append(F(state.Orientation.Z));
        sb.Append(']');

        // Airspeed (magnitude of velocity vector, m/s)
        double airspeed = Math.Sqrt(
            state.Velocity.X * state.Velocity.X +
            state.Velocity.Y * state.Velocity.Y +
            state.Velocity.Z * state.Velocity.Z);
        sb.Append(",\"airspeed\":");
        sb.Append(F(airspeed));

        sb.Append('}');
        return sb.ToString();
    }

    private static string F(double value) =>
        value.ToString("G9", CultureInfo.InvariantCulture);
}
