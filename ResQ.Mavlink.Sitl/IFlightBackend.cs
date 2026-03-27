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
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl;

/// <summary>
/// Abstraction over a drone flight backend — either a local physics model or a live SITL process.
/// Implementations advance drone physics state on each call to <see cref="StepAsync"/> and accept
/// high-level <see cref="FlightCommand"/> inputs via <see cref="SendCommandAsync"/>.
/// </summary>
public interface IFlightBackend : IAsyncDisposable
{
    /// <summary>
    /// Gets the capabilities supported by this backend.
    /// </summary>
    FlightBackendCapabilities Capabilities { get; }

    /// <summary>
    /// Initialises the backend with the given drone configuration.
    /// Must be called once before <see cref="StepAsync"/> or <see cref="SendCommandAsync"/>.
    /// </summary>
    /// <param name="config">Configuration describing the drone to simulate.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask InitializeAsync(DroneConfig config, CancellationToken ct = default);

    /// <summary>
    /// Advances the physics simulation by <paramref name="dt"/> seconds, applying the optional
    /// <paramref name="wind"/> disturbance, and returns the updated drone state.
    /// </summary>
    /// <param name="dt">Timestep duration in seconds (must be &gt; 0).</param>
    /// <param name="wind">World-space wind velocity in m/s applied additively to drone velocity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="DronePhysicsState"/> after the step.</returns>
    ValueTask<DronePhysicsState> StepAsync(double dt, Vector3 wind, CancellationToken ct = default);

    /// <summary>
    /// Sends a flight command to the backend. The command overrides any previously active command.
    /// </summary>
    /// <param name="command">The flight command to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask SendCommandAsync(FlightCommand command, CancellationToken ct = default);
}
