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
/// Controls how the simulation clock advances relative to wall-clock time.
/// </summary>
public enum ClockMode
{
    /// <summary>
    /// Clock advances in real-time — one wall-clock second equals one simulation second.
    /// </summary>
    RealTime,

    /// <summary>
    /// Clock advances at a multiple of real time determined by the acceleration factor.
    /// </summary>
    Accelerated,

    /// <summary>
    /// Clock advances by a fixed delta only when <see cref="ISimulationClock.Advance"/> is called explicitly.
    /// </summary>
    Stepped,
}

/// <summary>
/// Represents a simulation clock that tracks elapsed simulation time and controls tick advancement.
/// </summary>
public interface ISimulationClock
{
    /// <summary>
    /// Gets the total simulated time elapsed since the clock was started, in seconds.
    /// </summary>
    double ElapsedTime { get; }

    /// <summary>
    /// Gets the base fixed timestep duration, in seconds, used per tick.
    /// </summary>
    double DeltaTime { get; }

    /// <summary>
    /// Gets the clock mode governing how ticks are advanced.
    /// </summary>
    ClockMode Mode { get; }

    /// <summary>
    /// Gets a value indicating whether the clock is currently paused.
    /// When paused, calls to <see cref="Advance"/> are no-ops.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Advances the clock by one tick.
    /// In <see cref="ClockMode.Stepped"/> and <see cref="ClockMode.RealTime"/> modes this adds
    /// <see cref="DeltaTime"/> to <see cref="ElapsedTime"/>; in <see cref="ClockMode.Accelerated"/>
    /// mode it adds <c>DeltaTime * AccelerationFactor</c>.
    /// This method is a no-op when <see cref="IsPaused"/> is <c>true</c>.
    /// </summary>
    void Advance();

    /// <summary>
    /// Pauses the clock. Subsequent calls to <see cref="Advance"/> will not increment <see cref="ElapsedTime"/>.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes the clock after a pause, allowing <see cref="Advance"/> to increment <see cref="ElapsedTime"/> again.
    /// </summary>
    void Resume();
}
