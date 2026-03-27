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

using FluentAssertions;
using ResQ.Simulation.Engine.Core;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Core;

public class SimulationClockTests
{
    // 1. Stepped mode: single Advance increments ElapsedTime by DeltaTime
    [Fact]
    public void Advance_SteppedMode_IncrementsElapsedTimeByDeltaTime()
    {
        var clock = new SimulationClock(ClockMode.Stepped, deltaTime: 0.1);
        clock.Advance();
        clock.ElapsedTime.Should().BeApproximately(0.1, 1e-10);
    }

    // 2. Multiple advances accumulate correctly
    [Fact]
    public void Advance_MultipleAdvances_AccumulatesElapsedTime()
    {
        var clock = new SimulationClock(ClockMode.Stepped, deltaTime: 0.1);
        clock.Advance();
        clock.Advance();
        clock.Advance();
        clock.ElapsedTime.Should().BeApproximately(0.3, 1e-10);
    }

    // 3. Paused clock: Advance is a no-op
    [Fact]
    public void Advance_WhenPaused_DoesNotIncrementElapsedTime()
    {
        var clock = new SimulationClock(ClockMode.Stepped, deltaTime: 0.1);
        clock.Pause();
        clock.Advance();
        clock.ElapsedTime.Should().Be(0.0);
    }

    // 4. Resume after pause resumes advancing
    [Fact]
    public void Resume_AfterPause_AdvanceIncrementsElapsedTime()
    {
        var clock = new SimulationClock(ClockMode.Stepped, deltaTime: 0.1);
        clock.Pause();
        clock.Advance(); // no-op
        clock.Resume();
        clock.Advance();
        clock.ElapsedTime.Should().BeApproximately(0.1, 1e-10);
    }

    // 5. Default deltaTime is 1/60
    [Fact]
    public void Constructor_DefaultDeltaTime_IsOneOverSixty()
    {
        var clock = new SimulationClock(ClockMode.Stepped);
        clock.DeltaTime.Should().BeApproximately(1.0 / 60.0, 1e-10);
    }

    // 6. Invalid deltaTime (zero) throws ArgumentOutOfRangeException
    [Fact]
    public void Constructor_ZeroDeltaTime_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SimulationClock(ClockMode.Stepped, deltaTime: 0.0);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("deltaTime");
    }

    // 7. Negative deltaTime throws ArgumentOutOfRangeException
    [Fact]
    public void Constructor_NegativeDeltaTime_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SimulationClock(ClockMode.Stepped, deltaTime: -0.5);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("deltaTime");
    }

    // 8. Mode returns the configured clock mode
    [Fact]
    public void Mode_ReturnsConfiguredMode()
    {
        var clock = new SimulationClock(ClockMode.RealTime);
        clock.Mode.Should().Be(ClockMode.RealTime);
    }

    // 9. Initial state is not paused
    [Fact]
    public void IsPaused_Initially_IsFalse()
    {
        var clock = new SimulationClock(ClockMode.Stepped);
        clock.IsPaused.Should().BeFalse();
    }

    // 10. Initial elapsed time is zero
    [Fact]
    public void ElapsedTime_Initially_IsZero()
    {
        var clock = new SimulationClock(ClockMode.Stepped);
        clock.ElapsedTime.Should().Be(0.0);
    }

    // 11. Accelerated mode: Advance uses DeltaTime * AccelerationFactor
    [Fact]
    public void Advance_AcceleratedMode_UsesAccelerationFactor()
    {
        var clock = new SimulationClock(ClockMode.Accelerated, deltaTime: 0.1, accelerationFactor: 3.0);
        clock.Advance();
        clock.ElapsedTime.Should().BeApproximately(0.3, 1e-10);
    }

    // 12. EffectiveDeltaTime reflects factor in Accelerated mode
    [Fact]
    public void EffectiveDeltaTime_AcceleratedMode_ReturnsDeltaTimeTimesAccelerationFactor()
    {
        var clock = new SimulationClock(ClockMode.Accelerated, deltaTime: 0.1, accelerationFactor: 2.5);
        clock.EffectiveDeltaTime.Should().BeApproximately(0.25, 1e-10);
    }

    // 13. Stepped mode: EffectiveDeltaTime ignores acceleration factor
    [Fact]
    public void EffectiveDeltaTime_SteppedMode_IgnoresAccelerationFactor()
    {
        var clock = new SimulationClock(ClockMode.Stepped, deltaTime: 0.1, accelerationFactor: 5.0);
        clock.EffectiveDeltaTime.Should().BeApproximately(0.1, 1e-10);
    }

    // 14. Invalid acceleration factor (zero) throws ArgumentOutOfRangeException
    [Fact]
    public void Constructor_ZeroAccelerationFactor_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SimulationClock(ClockMode.Accelerated, deltaTime: 0.1, accelerationFactor: 0.0);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("accelerationFactor");
    }
}
