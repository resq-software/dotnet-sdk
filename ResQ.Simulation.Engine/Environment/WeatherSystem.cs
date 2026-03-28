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
/// A deterministic weather system that simulates calm, steady, and turbulent wind
/// conditions for use in drone flight simulations.
/// </summary>
/// <remarks>
/// <para>
/// Wind direction follows the compass convention: 0° = North, 90° = East, 180° = South,
/// 270° = West.  In world space, North maps to +Z and East maps to +X.
/// </para>
/// <para>
/// Altitude scaling: in <see cref="WeatherMode.Steady"/> and
/// <see cref="WeatherMode.Turbulent"/> modes, wind speed is multiplied by an altitude
/// factor clamped to a floor of 50 % of the base speed.  The reference altitude is
/// 50 m, above which wind speed increases by ~0.5 % per metre.
/// </para>
/// <para>
/// Turbulence is produced by a simple integer hash function seeded with
/// <see cref="WeatherConfig.TurbulenceSeed"/>, the quantised position, and the
/// elapsed time bucket, giving fully deterministic results for the same seed and
/// simulation trajectory.
/// </para>
/// </remarks>
public sealed class WeatherSystem : IWeatherSystem
{
    private const double ReferenceAltitude = 50.0;
    private const double AltitudeGradient = 0.005; // 0.5 % per metre
    private const double AltitudeFloor = 0.5;       // 50 % floor
    private const double TurbulenceFraction = 0.30; // 30 % of base speed

    private readonly WeatherConfig _config;
    private readonly Vector3 _baseWindDir; // unit vector in world space
    private double _time;

    /// <summary>
    /// Initialises a new <see cref="WeatherSystem"/> from the supplied configuration.
    /// </summary>
    /// <param name="config">The weather configuration to use.</param>
    public WeatherSystem(WeatherConfig config)
    {
        _config = config;
        _baseWindDir = DegreesToWorldVector(config.WindDirection);
    }

    /// <inheritdoc/>
    public double Visibility => _config.Visibility;

    /// <inheritdoc/>
    public double Precipitation => _config.Precipitation;

    /// <inheritdoc/>
    public Vector3 GetWind(double x, double y, double z)
    {
        return _config.Mode switch
        {
            WeatherMode.Calm => Vector3.Zero,
            WeatherMode.Steady => ComputeSteadyWind(y),
            WeatherMode.Turbulent => ComputeTurbulentWind(x, y, z),
            _ => Vector3.Zero,
        };
    }

    /// <inheritdoc/>
    public void Step(double dt) => _time += dt;

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private Vector3 ComputeSteadyWind(double altitude)
    {
        double scale = AltitudeFactor(altitude);
        return _baseWindDir * (float)(_config.WindSpeed * scale);
    }

    private Vector3 ComputeTurbulentWind(double x, double y, double z)
    {
        Vector3 steady = ComputeSteadyWind(y);

        // Gust magnitude: 30 % of base speed (not altitude-scaled)
        double gustMag = _config.WindSpeed * TurbulenceFraction;

        // Hash-based deterministic noise per axis
        double nx = HashNoise(_config.TurbulenceSeed, x, y, z, _time, 0);
        double ny = HashNoise(_config.TurbulenceSeed, x, y, z, _time, 1);
        double nz = HashNoise(_config.TurbulenceSeed, x, y, z, _time, 2);

        var gust = new Vector3(
            (float)(nx * gustMag),
            (float)(ny * gustMag * 0.3), // vertical gusts are smaller
            (float)(nz * gustMag));

        return steady + gust;
    }

    /// <summary>
    /// Computes the altitude-dependent wind speed scaling factor.
    /// At or below <see cref="ReferenceAltitude"/> the factor is 1.0; above it
    /// increases linearly by <see cref="AltitudeGradient"/> per metre, with a
    /// floor of <see cref="AltitudeFloor"/>.
    /// </summary>
    private static double AltitudeFactor(double altitude)
    {
        double excess = altitude - ReferenceAltitude;
        double factor = 1.0 + excess * AltitudeGradient;
        return Math.Max(AltitudeFloor, factor);
    }

    /// <summary>
    /// Maps a compass bearing in degrees to a unit direction vector in world space.
    /// North (0°) maps to +Z; East (90°) maps to +X.
    /// </summary>
    private static Vector3 DegreesToWorldVector(double degrees)
    {
        double radians = degrees * Math.PI / 180.0;
        // 0° → (0, 0, 1); 90° → (1, 0, 0)
        float windX = (float)Math.Sin(radians);
        float windZ = (float)Math.Cos(radians);
        return new Vector3(windX, 0f, windZ);
    }

    /// <summary>
    /// A lightweight deterministic hash that produces a value in [-1, 1] from the
    /// supplied spatial/temporal inputs and an axis discriminator.
    /// </summary>
    private static double HashNoise(int seed, double x, double y, double z, double t, int axis)
    {
        // Quantise to 10-metre / 1-second cells for stable interpolation
        int ix = (int)Math.Floor(x / 10.0);
        int iy = (int)Math.Floor(y / 10.0);
        int iz = (int)Math.Floor(z / 10.0);
        int it = (int)Math.Floor(t);

        unchecked
        {
            int h = seed;
            h = h * 1664525 + 1013904223;
            h ^= ix * (int)2246822519u;
            h ^= iy * (int)2654435761u;
            h ^= iz * (int)3266489917u;
            h ^= it * 668265263;
            h ^= axis * 374761393;
            h = h * 1664525 + 1013904223;
            h ^= h >> 16;
            h *= unchecked((int)0x45d9f3b);
            h ^= h >> 16;
            // Map to [-1, 1]
            return (h & 0x7FFFFFFF) / (double)0x7FFFFFFF * 2.0 - 1.0;
        }
    }
}
