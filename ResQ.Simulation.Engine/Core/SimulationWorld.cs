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
using Microsoft.Extensions.Options;
using ResQ.Simulation.Engine.Entities;
using ResQ.Simulation.Engine.Environment;

namespace ResQ.Simulation.Engine.Core;

/// <summary>
/// Top-level container for a running simulation, owning the clock, drones, structures,
/// terrain, and weather system.
/// </summary>
/// <remarks>
/// Advance the world by calling <see cref="Step"/> once per tick.  Each call advances the
/// <see cref="Clock"/>, steps the <see cref="Weather"/>, and then steps every non-landed drone
/// with the wind sampled at its current position.
/// </remarks>
public sealed class SimulationWorld
{
    private readonly List<SimulatedDrone> _drones = new();
    private readonly List<Structure> _structures = new();

    /// <summary>
    /// Initializes a new <see cref="SimulationWorld"/> from an <see cref="IOptions{TOptions}"/> wrapper.
    /// </summary>
    /// <param name="options">Wrapped simulation configuration.</param>
    /// <param name="terrain">The terrain model providing elevation queries.</param>
    /// <param name="weather">The weather system providing wind and visibility.</param>
    public SimulationWorld(IOptions<SimulationConfig> options, ITerrain terrain, IWeatherSystem weather)
        : this(options.Value, terrain, weather)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SimulationWorld"/> directly from a <see cref="SimulationConfig"/>.
    /// </summary>
    /// <param name="config">Simulation configuration (clock mode, timestep, seed, flight model).</param>
    /// <param name="terrain">The terrain model providing elevation queries.</param>
    /// <param name="weather">The weather system providing wind and visibility.</param>
    public SimulationWorld(SimulationConfig config, ITerrain terrain, IWeatherSystem weather)
    {
        Clock = new SimulationClock(config.ClockMode, config.DeltaTime, config.AccelerationFactor);
        Terrain = terrain;
        Weather = weather;
        Random = new Random(config.Seed);
    }

    /// <summary>Gets the simulation clock that tracks elapsed simulation time.</summary>
    public SimulationClock Clock { get; }

    /// <summary>Gets the ordered list of drones currently registered in the world.</summary>
    public IReadOnlyList<SimulatedDrone> Drones => _drones;

    /// <summary>Gets the ordered list of structures currently registered in the world.</summary>
    public IReadOnlyList<Structure> Structures => _structures;

    /// <summary>Gets the terrain model used for elevation queries.</summary>
    public ITerrain Terrain { get; }

    /// <summary>Gets the weather system that supplies wind and visibility data.</summary>
    public IWeatherSystem Weather { get; }

    /// <summary>
    /// Gets the seeded random number generator for deterministic simulation outcomes.
    /// Seed is taken from <see cref="SimulationConfig.Seed"/>.
    /// </summary>
    public Random Random { get; }

    /// <summary>
    /// Adds a new drone to the world at the specified start position, using the flight model
    /// configured in <see cref="SimulationConfig.FlightModel"/>.
    /// </summary>
    /// <param name="id">A unique, non-whitespace identifier for the drone.</param>
    /// <param name="startPosition">The world-space launch position.</param>
    /// <returns>The newly created <see cref="SimulatedDrone"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is <see langword="null"/>, whitespace, or already registered.
    /// </exception>
    public SimulatedDrone AddDrone(string id, Vector3 startPosition)
    {
        if (_drones.Exists(d => d.Id == id))
            throw new ArgumentException($"A drone with id '{id}' already exists.", nameof(id));

        var drone = new SimulatedDrone(id, startPosition, FlightModelType.Kinematic);
        _drones.Add(drone);
        return drone;
    }

    /// <summary>
    /// Adds a new structure to the world.
    /// </summary>
    /// <param name="id">A unique, non-whitespace identifier for the structure.</param>
    /// <param name="position">World-space centre position in metres.</param>
    /// <param name="halfExtents">Half-extents of the axis-aligned bounding box in metres.</param>
    /// <returns>The newly created <see cref="Structure"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is <see langword="null"/>, whitespace, or already registered.
    /// </exception>
    public Structure AddStructure(string id, Vector3 position, Vector3 halfExtents)
    {
        if (_structures.Exists(s => s.Id == id))
            throw new ArgumentException($"A structure with id '{id}' already exists.", nameof(id));

        var structure = new Structure(id, position, halfExtents);
        _structures.Add(structure);
        return structure;
    }

    /// <summary>
    /// Advances the world by one fixed timestep.
    /// </summary>
    /// <remarks>
    /// The step sequence is:
    /// <list type="number">
    ///   <item><description>Check whether the clock is currently paused.</description></item>
    ///   <item><description>Call <see cref="SimulationClock.Advance"/> (no-op when paused).</description></item>
    ///   <item><description>If the clock was paused, return early — drones and weather are not advanced.</description></item>
    ///   <item><description>Step the weather system by <see cref="SimulationClock.EffectiveDeltaTime"/>.</description></item>
    ///   <item><description>For each non-landed drone, sample wind at the drone's position and call <see cref="SimulatedDrone.Step"/>.</description></item>
    /// </list>
    /// </remarks>
    public void Step()
    {
        var wasPaused = Clock.IsPaused;
        Clock.Advance();

        if (wasPaused)
            return;

        var dt = Clock.EffectiveDeltaTime;
        Weather.Step(dt);

        foreach (var drone in _drones)
        {
            if (drone.FlightModel.HasLanded)
                continue;

            var pos = drone.FlightModel.State.Position;
            var wind = Weather.GetWind(pos.X, pos.Y, pos.Z);
            drone.Step(dt, wind);
        }
    }
}
