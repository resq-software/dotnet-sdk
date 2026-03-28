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
using FluentAssertions;
using ResQ.Simulation.Engine.Environment;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Environment;

public class WeatherSystemTests
{
    // 1. Calm mode: GetWind always returns zero
    [Fact]
    public void GetWind_CalmMode_ReturnsZeroVector()
    {
        var config = new WeatherConfig(Mode: WeatherMode.Calm, WindSpeed: 10);
        var weather = new WeatherSystem(config);

        var wind = weather.GetWind(0, 50, 0);

        wind.Should().Be(Vector3.Zero);
    }

    // 2. Steady mode with North wind: wind blows toward +Z (South)
    [Fact]
    public void GetWind_SteadyNorthWind_BlowsInPositiveZDirection()
    {
        // 0° = North wind blows toward South (+Z in world space)
        var config = new WeatherConfig(Mode: WeatherMode.Steady, WindDirection: 0, WindSpeed: 10);
        var weather = new WeatherSystem(config);

        var wind = weather.GetWind(0, 50, 0); // altitude = reference altitude → scale = 1

        wind.X.Should().BeApproximately(0f, 1e-5f);
        wind.Z.Should().BeApproximately(10f, 1e-4f);
        wind.Y.Should().BeApproximately(0f, 1e-5f);
    }

    // 3. Steady mode with East wind: wind blows toward +X
    [Fact]
    public void GetWind_SteadyEastWind_BlowsInPositiveXDirection()
    {
        // 90° = East wind
        var config = new WeatherConfig(Mode: WeatherMode.Steady, WindDirection: 90, WindSpeed: 8);
        var weather = new WeatherSystem(config);

        var wind = weather.GetWind(0, 50, 0);

        wind.X.Should().BeApproximately(8f, 1e-4f);
        wind.Z.Should().BeApproximately(0f, 1e-5f);
    }

    // 4. Turbulent mode: wind varies over time after stepping
    [Fact]
    public void GetWind_TurbulentMode_ChangesOverTime()
    {
        var config = new WeatherConfig(
            Mode: WeatherMode.Turbulent,
            WindDirection: 0,
            WindSpeed: 10,
            TurbulenceSeed: 42);
        var weather = new WeatherSystem(config);

        var windBefore = weather.GetWind(0, 50, 0);
        weather.Step(10.0); // advance 10 seconds to change time bucket
        var windAfter = weather.GetWind(0, 50, 0);

        windBefore.Should().NotBe(windAfter);
    }

    // 5. Same seed produces same turbulence results
    [Fact]
    public void GetWind_TurbulentMode_SameSeedProducesSameResult()
    {
        var config = new WeatherConfig(
            Mode: WeatherMode.Turbulent,
            WindDirection: 45,
            WindSpeed: 5,
            TurbulenceSeed: 99);

        var weather1 = new WeatherSystem(config);
        weather1.Step(3.7);
        var wind1 = weather1.GetWind(12, 60, 34);

        var weather2 = new WeatherSystem(config);
        weather2.Step(3.7);
        var wind2 = weather2.GetWind(12, 60, 34);

        wind1.Should().Be(wind2);
    }

    // 6. Visibility returns the configured value
    [Fact]
    public void Visibility_ReturnsConfiguredValue()
    {
        var config = new WeatherConfig(Visibility: 0.6);
        var weather = new WeatherSystem(config);

        weather.Visibility.Should().BeApproximately(0.6, 1e-10);
    }

    // 7. Precipitation returns the configured value
    [Fact]
    public void Precipitation_ReturnsConfiguredValue()
    {
        var config = new WeatherConfig(Precipitation: 0.3);
        var weather = new WeatherSystem(config);

        weather.Precipitation.Should().BeApproximately(0.3, 1e-10);
    }

    // 8. Wind speed increases with altitude in Steady mode (above reference)
    [Fact]
    public void GetWind_SteadyMode_WindIncreasesWithAltitude()
    {
        var config = new WeatherConfig(Mode: WeatherMode.Steady, WindDirection: 0, WindSpeed: 10);
        var weather = new WeatherSystem(config);

        var windLow = weather.GetWind(0, 50, 0);   // reference altitude
        var windHigh = weather.GetWind(0, 200, 0);  // well above reference

        var speedLow = windLow.Length();
        var speedHigh = windHigh.Length();

        speedHigh.Should().BeGreaterThan(speedLow);
    }
}
