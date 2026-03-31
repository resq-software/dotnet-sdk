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
using ResQ.Simulation.Engine.Core;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Simulation.Engine.Entities;

/// <summary>
/// Represents a simulated drone in the simulation world, encapsulating a flight model,
/// telemetry tracking, and detection probability logic.
/// </summary>
public sealed class SimulatedDrone
{
    private const double BaseDetectionProbability = 0.05;
    private const double DefaultQuadrotorMass = 2.5;

    /// <summary>Gets the unique identifier for this drone.</summary>
    public string Id { get; }

    /// <summary>
    /// Gets the flight model backing this drone's physics simulation.
    /// The concrete type depends on the <see cref="FlightModelType"/> passed to the constructor.
    /// </summary>
    public IFlightModel FlightModel { get; }

    /// <summary>
    /// Gets the total number of telemetry ticks that have been recorded for this drone
    /// (incremented once per <see cref="Step"/> call).
    /// </summary>
    public int TelemetryCount { get; private set; }

    /// <summary>
    /// Gets or sets the cumulative number of times this drone has been detected by
    /// external observers during the simulation.
    /// </summary>
    public int DetectionCount { get; set; }

    /// <summary>
    /// Initializes a new <see cref="SimulatedDrone"/>.
    /// </summary>
    /// <param name="id">A non-null, non-whitespace identifier that uniquely names this drone.</param>
    /// <param name="startPosition">The world-space launch position.</param>
    /// <param name="modelType">
    /// Selects the underlying <see cref="IFlightModel"/> implementation:
    /// <see cref="FlightModelType.Kinematic"/> for lightweight integration,
    /// or <see cref="FlightModelType.Quadrotor"/> for full 6-DOF dynamics.
    /// </param>
    /// <param name="mass">
    /// The drone mass in kilograms. Only used when <paramref name="modelType"/> is
    /// <see cref="FlightModelType.Quadrotor"/>. Defaults to <c>2.5 kg</c>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is <see langword="null"/> or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="modelType"/> is not a recognised <see cref="FlightModelType"/> value.
    /// </exception>
    public SimulatedDrone(string id, Vector3 startPosition, FlightModelType modelType, double mass = DefaultQuadrotorMass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        Id = id;
        FlightModel = modelType switch
        {
            FlightModelType.Kinematic => new KinematicFlightModel(startPosition),
            FlightModelType.Quadrotor => new QuadrotorFlightModel(startPosition, mass),
            _ => throw new ArgumentOutOfRangeException(nameof(modelType))
        };
    }

    /// <summary>
    /// Forwards <paramref name="command"/> to the underlying <see cref="FlightModel"/>.
    /// </summary>
    /// <param name="command">The flight command to apply.</param>
    public void SendCommand(FlightCommand command) => FlightModel.ApplyCommand(command);

    /// <summary>
    /// Advances the drone's physics state by one timestep and increments <see cref="TelemetryCount"/>.
    /// </summary>
    /// <param name="dt">The timestep duration in seconds.</param>
    /// <param name="wind">The wind disturbance vector (world space, m/s) for this tick.</param>
    public void Step(double dt, Vector3 wind)
    {
        FlightModel.Step(dt, wind);
        TelemetryCount++;
    }

    /// <summary>
    /// Returns the probability that this drone is detected by an observer given the current
    /// atmospheric visibility.
    /// </summary>
    /// <param name="visibility">
    /// Normalised visibility in [0, 1]; <c>1</c> = perfect visibility, <c>0</c> = zero visibility.
    /// </param>
    /// <returns>
    /// A detection probability in [0, <see cref="BaseDetectionProbability"/>], linearly scaled
    /// by <paramref name="visibility"/>.
    /// </returns>
    public double GetDetectionProbability(double visibility) =>
        BaseDetectionProbability * Math.Clamp(visibility, 0.0, 1.0);
}
