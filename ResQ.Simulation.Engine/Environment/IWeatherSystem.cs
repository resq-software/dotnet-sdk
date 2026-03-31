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

namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// Models atmospheric conditions and provides per-position wind vectors for use
/// by flight model implementations.
/// </summary>
public interface IWeatherSystem
{
    /// <summary>
    /// Gets the current atmospheric visibility as a normalised scalar in [0, 1].
    /// A value of <c>1</c> represents perfect visibility; <c>0</c> means zero visibility.
    /// </summary>
    double Visibility { get; }

    /// <summary>
    /// Gets the current precipitation intensity as a normalised scalar in [0, 1].
    /// A value of <c>0</c> is dry; <c>1</c> is maximum precipitation intensity.
    /// </summary>
    double Precipitation { get; }

    /// <summary>
    /// Returns the wind velocity vector at the specified world-space position.
    /// </summary>
    /// <param name="x">The X (East) coordinate in metres.</param>
    /// <param name="y">The Y (Up) coordinate — altitude — in metres.</param>
    /// <param name="z">The Z (South) coordinate in metres.</param>
    /// <returns>
    /// Wind velocity in metres per second in world space (X = East, Y = Up, Z = South).
    /// </returns>
    Vector3 GetWind(double x, double y, double z);

    /// <summary>
    /// Advances the internal simulation time by <paramref name="dt"/> seconds.
    /// This drives the evolution of turbulence patterns over time.
    /// </summary>
    /// <param name="dt">The timestep duration in seconds.  Must be greater than zero.</param>
    void Step(double dt);
}
