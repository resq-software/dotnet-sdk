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
/// Default implementation of <see cref="ISimulationClock"/> supporting stepped, real-time,
/// and accelerated clock modes.
/// </summary>
public sealed class SimulationClock : ISimulationClock
{
    /// <summary>
    /// Initializes a new <see cref="SimulationClock"/> instance.
    /// </summary>
    /// <param name="mode">The clock mode that controls how ticks advance.</param>
    /// <param name="deltaTime">
    /// The fixed timestep duration in seconds per tick. Must be greater than zero.
    /// Defaults to <c>1/60</c> (~16.67 ms).
    /// </param>
    /// <param name="accelerationFactor">
    /// The multiplier applied to <paramref name="deltaTime"/> when <paramref name="mode"/> is
    /// <see cref="ClockMode.Accelerated"/>. Must be greater than zero. Defaults to <c>1.0</c>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="deltaTime"/> or <paramref name="accelerationFactor"/> is
    /// less than or equal to zero.
    /// </exception>
    public SimulationClock(
        ClockMode mode,
        double deltaTime = 1.0 / 60.0,
        double accelerationFactor = 1.0)
    {
        if (deltaTime <= 0)
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime,
                "deltaTime must be greater than zero.");

        if (accelerationFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(accelerationFactor), accelerationFactor,
                "accelerationFactor must be greater than zero.");

        Mode = mode;
        DeltaTime = deltaTime;
        AccelerationFactor = accelerationFactor;
        ElapsedTime = 0.0;
        IsPaused = false;
    }

    /// <inheritdoc />
    public double ElapsedTime { get; private set; }

    /// <inheritdoc />
    public double DeltaTime { get; }

    /// <inheritdoc />
    public ClockMode Mode { get; }

    /// <inheritdoc />
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Gets the acceleration multiplier. Only applied to <see cref="EffectiveDeltaTime"/>
    /// when <see cref="Mode"/> is <see cref="ClockMode.Accelerated"/>.
    /// </summary>
    public double AccelerationFactor { get; }

    /// <summary>
    /// Gets the effective timestep used by <see cref="Advance"/>:
    /// <c>DeltaTime * AccelerationFactor</c> in <see cref="ClockMode.Accelerated"/> mode,
    /// <c>DeltaTime</c> otherwise.
    /// </summary>
    public double EffectiveDeltaTime =>
        Mode == ClockMode.Accelerated ? DeltaTime * AccelerationFactor : DeltaTime;

    /// <inheritdoc />
    public void Advance()
    {
        if (IsPaused)
            return;

        ElapsedTime += EffectiveDeltaTime;
    }

    /// <inheritdoc />
    public void Pause() => IsPaused = true;

    /// <inheritdoc />
    public void Resume() => IsPaused = false;
}
