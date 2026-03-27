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

namespace ResQ.Simulation.Engine.Core;

/// <summary>
/// Specifies the flight model used for drone physics simulation.
/// </summary>
public enum FlightModelType
{
    /// <summary>
    /// Simplified kinematic model — position/velocity integration without aerodynamic forces.
    /// Suitable for fast, approximate simulations.
    /// </summary>
    Kinematic,

    /// <summary>
    /// Full quadrotor dynamics model with rotor thrust, torque, and drag.
    /// Suitable for high-fidelity simulations.
    /// </summary>
    Quadrotor,
}

/// <summary>
/// Configuration options for the simulation engine, designed for use with
/// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>.
/// </summary>
public sealed class SimulationConfig
{
    /// <summary>
    /// Gets or sets the fixed simulation timestep in seconds.
    /// Defaults to <c>1/60</c> (~16.67 ms per tick).
    /// </summary>
    public double DeltaTime { get; set; } = 1.0 / 60.0;

    /// <summary>
    /// Gets or sets the random seed used to initialise deterministic simulation components.
    /// Defaults to <c>42</c>.
    /// </summary>
    public int Seed { get; set; } = 42;

    /// <summary>
    /// Gets or sets the clock mode that governs how simulation time advances.
    /// Defaults to <see cref="ClockMode.Stepped"/>.
    /// </summary>
    public ClockMode ClockMode { get; set; } = ClockMode.Stepped;

    /// <summary>
    /// Gets or sets the acceleration factor applied to simulation time when
    /// <see cref="ClockMode"/> is <see cref="ClockMode.Accelerated"/>.
    /// A value of <c>2.0</c> means the simulation runs at double real-time speed.
    /// Defaults to <c>1.0</c>.
    /// </summary>
    public double AccelerationFactor { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the flight model used for drone physics.
    /// Defaults to <see cref="FlightModelType.Kinematic"/>.
    /// </summary>
    public FlightModelType FlightModel { get; set; } = FlightModelType.Kinematic;
}
