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

namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// Governs how wind turbulence and gust variation is calculated by <see cref="WeatherSystem"/>.
/// </summary>
public enum WeatherMode
{
    /// <summary>
    /// No wind.  <see cref="IWeatherSystem.GetWind"/> always returns the zero vector.
    /// </summary>
    Calm,

    /// <summary>
    /// Constant base wind scaled by altitude with no random variation.
    /// </summary>
    Steady,

    /// <summary>
    /// Steady base wind plus deterministic hash-based gust noise that varies with
    /// position and elapsed simulation time.
    /// </summary>
    Turbulent,
}

/// <summary>
/// Configuration record for a <see cref="WeatherSystem"/> instance, intended for
/// use with <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>.
/// </summary>
/// <param name="Mode">
/// Wind simulation mode.  Defaults to <see cref="WeatherMode.Calm"/>.
/// </param>
/// <param name="WindDirection">
/// Compass bearing of the wind origin in degrees (0 = North, 90 = East, 180 = South, 270 = West).
/// The wind blows <em>toward</em> the opposite direction (i.e. a North wind blows toward South).
/// Defaults to <c>0</c>.
/// </param>
/// <param name="WindSpeed">
/// Base wind speed in metres per second at the reference altitude.  Defaults to <c>0</c>.
/// </param>
/// <param name="Visibility">
/// Atmospheric visibility as a normalised scalar in the range [0, 1] where 1 represents
/// perfect visibility.  Defaults to <c>1.0</c>.
/// </param>
/// <param name="Precipitation">
/// Precipitation intensity as a normalised scalar in the range [0, 1] where 0 is dry and
/// 1 is maximum precipitation.  Defaults to <c>0.0</c>.
/// </param>
/// <param name="TurbulenceSeed">
/// Integer seed that initialises the deterministic turbulence noise generator.
/// The same seed always produces the same gust pattern.  Defaults to <c>0</c>.
/// </param>
public sealed record WeatherConfig(
    WeatherMode Mode = WeatherMode.Calm,
    double WindDirection = 0,
    double WindSpeed = 0,
    double Visibility = 1.0,
    double Precipitation = 0.0,
    int TurbulenceSeed = 0);
