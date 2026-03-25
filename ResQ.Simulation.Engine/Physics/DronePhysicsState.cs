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

namespace ResQ.Simulation.Engine.Physics;

/// <summary>
/// Represents the full kinematic and power state of a simulated drone at a single instant.
/// </summary>
/// <param name="Position">World-space position in metres (X = East, Y = Up, Z = South).</param>
/// <param name="Velocity">World-space velocity in metres per second.</param>
/// <param name="Orientation">Body orientation as a unit quaternion.</param>
/// <param name="AngularVelocity">Angular velocity vector in radians per second.</param>
/// <param name="BatteryPercent">Remaining battery charge as a percentage in the range [0, 100].</param>
public readonly record struct DronePhysicsState(
    Vector3 Position,
    Vector3 Velocity,
    Quaternion Orientation,
    Vector3 AngularVelocity,
    double BatteryPercent)
{
    /// <summary>
    /// Creates a <see cref="DronePhysicsState"/> with the drone placed at <paramref name="position"/>,
    /// at rest (zero velocity, zero angular velocity), with identity orientation, and full battery.
    /// </summary>
    /// <param name="position">The initial world-space position for the drone.</param>
    /// <returns>A new <see cref="DronePhysicsState"/> representing the at-rest initial condition.</returns>
    public static DronePhysicsState AtPosition(Vector3 position) =>
        new(position, Vector3.Zero, Quaternion.Identity, Vector3.Zero, 100.0);
}
