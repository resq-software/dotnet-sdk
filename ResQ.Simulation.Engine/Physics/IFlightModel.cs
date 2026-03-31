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
/// Defines the contract for a drone flight model that integrates physics state over time.
/// </summary>
/// <remarks>
/// Implementations receive discrete control commands via <see cref="ApplyCommand"/> and advance
/// the physics simulation one timestep at a time via <see cref="Step"/>.
/// </remarks>
public interface IFlightModel
{
    /// <summary>
    /// Gets the current kinematic and power state of the drone.
    /// </summary>
    DronePhysicsState State { get; }

    /// <summary>
    /// Gets the world-space position at which the drone was launched.
    /// Used as the destination when a <see cref="FlightCommandType.ReturnToLaunch"/> command is issued.
    /// </summary>
    Vector3 LaunchPosition { get; }

    /// <summary>
    /// Gets a value indicating whether the drone has completed a landing sequence and is on the ground.
    /// Once <see langword="true"/>, subsequent calls to <see cref="Step"/> are no-ops.
    /// </summary>
    bool HasLanded { get; }

    /// <summary>
    /// Queues or immediately applies <paramref name="command"/> to the flight model,
    /// overriding the previously active command.
    /// </summary>
    /// <param name="command">The flight command to execute.</param>
    void ApplyCommand(FlightCommand command);

    /// <summary>
    /// Advances the flight model by <paramref name="dt"/> seconds, integrating forces,
    /// velocity, and position, optionally disturbed by <paramref name="wind"/>.
    /// </summary>
    /// <param name="dt">
    /// The timestep duration in seconds. Must be greater than zero.
    /// </param>
    /// <param name="wind">
    /// A wind disturbance vector in metres per second (world space) applied additively to the drone's
    /// computed velocity during this step.
    /// </param>
    void Step(double dt, Vector3 wind);
}
