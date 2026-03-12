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
using ResQ.Simulation;
using Xunit;

namespace ResQ.Simulation.Tests;

public class ScenarioRunnerTests
{
    [Fact]
    public void Constructor_WithValidUrls_ShouldNotThrow()
    {
        var act = () => new ScenarioRunner("http://localhost:3000", "http://localhost:5000");
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullHceUrl_ShouldThrow()
    {
        var act = () => new ScenarioRunner(null!, "http://localhost:5000");
        act.Should().Throw<ArgumentNullException>().WithParameterName("hceUrl");
    }

    [Fact]
    public void Constructor_WithNullInfraUrl_ShouldThrow()
    {
        var act = () => new ScenarioRunner("http://localhost:3000", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("infraUrl");
    }

    [Fact]
    public void Constructor_WithInvalidHceUrl_ShouldThrow()
    {
        var act = () => new ScenarioRunner("not-a-url", "http://localhost:5000");
        act.Should().Throw<ArgumentException>().WithParameterName("hceUrl");
    }

    [Fact]
    public void Constructor_WithInvalidInfraUrl_ShouldThrow()
    {
        var act = () => new ScenarioRunner("http://localhost:3000", "not-a-url");
        act.Should().Throw<ArgumentException>().WithParameterName("infraUrl");
    }

    [Fact]
    public void Constructor_WithDefaultUrls_ShouldNotThrow()
    {
        var act = () => new ScenarioRunner();
        act.Should().NotThrow();
    }
}
