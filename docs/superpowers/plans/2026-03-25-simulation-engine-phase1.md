# Simulation Engine Phase 1: Core + Physics + Basic Environment

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the foundational simulation engine with clock management, dual flight models (kinematic + quadrotor), terrain, weather, and a working sim loop that can step drones through an environment deterministically. BepuPhysics2 collision and hazard systems are deferred to Phase 2.

**Architecture:** Pure .NET 9 library (`ResQ.Simulation.Engine`) with no external runtime dependencies. The engine owns the simulation loop, physics, and environment. It references `ResQ.Core` for shared models. Tests use stepped clock mode for deterministic assertions.

**Tech Stack:** .NET 9, System.Numerics (Vector3/Quaternion), xUnit, FluentAssertions, NSubstitute

**Deferred to Phase 2:** BepuPhysics2 collision system (`CollisionSystem.cs`), gRPC API, hazard simulations, scenario loading, headless runner.

**Spec:** `docs/superpowers/specs/2026-03-25-simulation-suite-design.md`

---

## File Map

### New Projects

| File | Responsibility |
|------|---------------|
| `ResQ.Simulation.Engine/ResQ.Simulation.Engine.csproj` | Project file, references ResQ.Core |
| `tests/ResQ.Simulation.Engine.Tests/ResQ.Simulation.Engine.Tests.csproj` | Test project |

### Core (Clock + World + Config)

| File | Responsibility |
|------|---------------|
| `ResQ.Simulation.Engine/Core/ISimulationClock.cs` | Clock interface: Advance, ElapsedTime, Mode |
| `ResQ.Simulation.Engine/Core/SimulationClock.cs` | Three-mode clock: RealTime, Accelerated, Stepped |
| `ResQ.Simulation.Engine/Core/SimulationConfig.cs` | IOptions<T> config: timestep, seed, clock mode, flight model |
| `ResQ.Simulation.Engine/Core/SimulationWorld.cs` | Top-level container: entities, environment, clock, Step() |
| `tests/ResQ.Simulation.Engine.Tests/Core/SimulationClockTests.cs` | Clock tests |
| `tests/ResQ.Simulation.Engine.Tests/Core/SimulationWorldTests.cs` | World/loop tests |

### Physics (Flight Models + Collision)

| File | Responsibility |
|------|---------------|
| `ResQ.Simulation.Engine/Physics/DronePhysicsState.cs` | Immutable state record: position, velocity, orientation, battery |
| `ResQ.Simulation.Engine/Physics/FlightCommand.cs` | Command types: GoToWaypoint, RTL, Land, Hover |
| `ResQ.Simulation.Engine/Physics/IFlightModel.cs` | Interface: State, ApplyCommand, Step(dt, wind) |
| `ResQ.Simulation.Engine/Physics/KinematicFlightModel.cs` | Fast point-to-point with speed/turn constraints |
| `ResQ.Simulation.Engine/Physics/QuadrotorFlightModel.cs` | 6DOF rigid body with thrust, drag, wind |
| `tests/ResQ.Simulation.Engine.Tests/Physics/KinematicFlightModelTests.cs` | Kinematic model tests |
| `tests/ResQ.Simulation.Engine.Tests/Physics/QuadrotorFlightModelTests.cs` | Quadrotor model tests |

> **Deferred:** `CollisionSystem.cs` (BepuPhysics2 wrapper) moves to Phase 2 when rigid body collision with terrain/structures is needed.

### Environment (Terrain + Weather)

| File | Responsibility |
|------|---------------|
| `ResQ.Simulation.Engine/Environment/ITerrain.cs` | Interface: GetElevation, GetSurfaceType |
| `ResQ.Simulation.Engine/Environment/FlatTerrain.cs` | Built-in flat terrain at configurable elevation |
| `ResQ.Simulation.Engine/Environment/HeightmapTerrain.cs` | Heightmap-based terrain from float arrays |
| `ResQ.Simulation.Engine/Environment/SurfaceType.cs` | Enum: Vegetation, Water, Urban, BareGround |
| `ResQ.Simulation.Engine/Environment/IWeatherSystem.cs` | Interface: GetWind, Visibility, Precipitation, Step(dt) |
| `ResQ.Simulation.Engine/Environment/WeatherSystem.cs` | Calm/Steady/Turbulent modes with Perlin wind |
| `ResQ.Simulation.Engine/Environment/WeatherConfig.cs` | Config record for weather mode/params |
| `tests/ResQ.Simulation.Engine.Tests/Environment/HeightmapTerrainTests.cs` | Terrain query tests |
| `tests/ResQ.Simulation.Engine.Tests/Environment/WeatherSystemTests.cs` | Weather/wind tests |

### Entities

| File | Responsibility |
|------|---------------|
| `ResQ.Simulation.Engine/Entities/SimulatedDrone.cs` | Drone entity: owns IFlightModel, sensors, battery |
| `ResQ.Simulation.Engine/Entities/Structure.cs` | Building/infrastructure with damage states |
| `tests/ResQ.Simulation.Engine.Tests/Entities/SimulatedDroneTests.cs` | Drone entity tests |

### Modified Files

| File | Change |
|------|--------|
| `Directory.Packages.props` | Add BepuPhysics2, NSubstitute, Grpc.AspNetCore package versions |
| `ResQ.Sdk.sln` | Add new projects to solution |

---

## Conventions

All new files must have the Apache-2.0 license header:

```csharp
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
```

- All public APIs must have XML doc comments.
- Use `IOptions<T>` for configuration.
- Nullable reference types enabled (inherited from `Directory.Build.props`).
- Tests use xUnit + FluentAssertions + NSubstitute (per CLAUDE.md).
- Pin package versions in `Directory.Packages.props`.

---

## Chunk 1: Project Scaffolding + Clock

### Task 1: Create project files and add to solution

**Files:**
- Create: `ResQ.Simulation.Engine/ResQ.Simulation.Engine.csproj`
- Create: `tests/ResQ.Simulation.Engine.Tests/ResQ.Simulation.Engine.Tests.csproj`
- Modify: `Directory.Packages.props`
- Modify: `ResQ.Sdk.sln`

- [ ] **Step 1: Add package versions to Directory.Packages.props**

Add these entries to the `<ItemGroup>` in `Directory.Packages.props`:

```xml
<!-- Simulation Engine -->
<PackageVersion Include="NSubstitute" Version="5.3.0" />
```

> **Deferred:** BepuPhysics2 and Grpc.AspNetCore will be added in Phase 2 when collision and gRPC API are implemented.

- [ ] **Step 2: Create ResQ.Simulation.Engine.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <PackageId>ResQ.Simulation.Engine</PackageId>
    <Version>0.1.0</Version>
    <Description>ResQ simulation engine — physics, environment, and scenario execution</Description>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/resq-software/dotnet-sdk</PackageProjectUrl>
    <RepositoryUrl>https://github.com/resq-software/dotnet-sdk</RepositoryUrl>
    <PackageTags>resq;drone;simulation;physics;disaster-response</PackageTags>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../ResQ.Core/ResQ.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Options" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create test project csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RollForward>LatestMajor</RollForward>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\ResQ.Simulation.Engine\ResQ.Simulation.Engine.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Add projects to solution**

Run:
```bash
dotnet sln ResQ.Sdk.sln add ResQ.Simulation.Engine/ResQ.Simulation.Engine.csproj
dotnet sln ResQ.Sdk.sln add tests/ResQ.Simulation.Engine.Tests/ResQ.Simulation.Engine.Tests.csproj --solution-folder tests
```

- [ ] **Step 5: Verify build**

Run: `dotnet build -c Release`
Expected: Build succeeds with no errors

- [ ] **Step 6: Commit**

```bash
git add Directory.Packages.props ResQ.Simulation.Engine/ tests/ResQ.Simulation.Engine.Tests/ ResQ.Sdk.sln
git commit -m "feat(sim-engine): scaffold ResQ.Simulation.Engine project and test project"
```

---

### Task 2: Implement ISimulationClock and SimulationClock

**Files:**
- Create: `ResQ.Simulation.Engine/Core/ISimulationClock.cs`
- Create: `ResQ.Simulation.Engine/Core/SimulationClock.cs`
- Create: `tests/ResQ.Simulation.Engine.Tests/Core/SimulationClockTests.cs`

- [ ] **Step 1: Write the clock interface**

Create `ResQ.Simulation.Engine/Core/ISimulationClock.cs`:

```csharp
// (license header)
namespace ResQ.Simulation.Engine.Core;

/// <summary>
/// Defines the simulation clock modes.
/// </summary>
public enum ClockMode
{
    /// <summary>1:1 wall clock time.</summary>
    RealTime,

    /// <summary>Accelerated by a configurable factor.</summary>
    Accelerated,

    /// <summary>Manual stepping — fully deterministic.</summary>
    Stepped
}

/// <summary>
/// Controls time progression in the simulation.
/// </summary>
public interface ISimulationClock
{
    /// <summary>Current simulation time in seconds since start.</summary>
    double ElapsedTime { get; }

    /// <summary>The fixed timestep in seconds (e.g. 1/60).</summary>
    double DeltaTime { get; }

    /// <summary>The active clock mode.</summary>
    ClockMode Mode { get; }

    /// <summary>Whether the clock is currently paused.</summary>
    bool IsPaused { get; }

    /// <summary>Advances the clock by one timestep. In RealTime/Accelerated mode, blocks until the wall-clock interval elapses.</summary>
    void Advance();

    /// <summary>Pauses the clock.</summary>
    void Pause();

    /// <summary>Resumes the clock.</summary>
    void Resume();
}
```

- [ ] **Step 2: Write failing tests for SimulationClock**

Create `tests/ResQ.Simulation.Engine.Tests/Core/SimulationClockTests.cs`:

```csharp
// (license header)
using FluentAssertions;
using ResQ.Simulation.Engine.Core;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Core;

public class SimulationClockTests
{
    [Fact]
    public void Stepped_Advance_IncrementsElapsedTime()
    {
        var clock = new SimulationClock(ClockMode.Stepped, deltaTime: 1.0 / 60.0);

        clock.Advance();

        clock.ElapsedTime.Should().BeApproximately(1.0 / 60.0, 1e-9);
    }

    [Fact]
    public void Stepped_MultipleAdvances_AccumulateTime()
    {
        var clock = new SimulationClock(ClockMode.Stepped, deltaTime: 1.0 / 60.0);

        clock.Advance();
        clock.Advance();
        clock.Advance();

        clock.ElapsedTime.Should().BeApproximately(3.0 / 60.0, 1e-9);
    }

    [Fact]
    public void Stepped_WhenPaused_AdvanceDoesNotIncrement()
    {
        var clock = new SimulationClock(ClockMode.Stepped, deltaTime: 1.0 / 60.0);

        clock.Advance();
        clock.Pause();
        clock.Advance();

        clock.ElapsedTime.Should().BeApproximately(1.0 / 60.0, 1e-9);
    }

    [Fact]
    public void Paused_Resume_AllowsAdvance()
    {
        var clock = new SimulationClock(ClockMode.Stepped, deltaTime: 1.0 / 60.0);

        clock.Pause();
        clock.IsPaused.Should().BeTrue();

        clock.Resume();
        clock.IsPaused.Should().BeFalse();

        clock.Advance();
        clock.ElapsedTime.Should().BeApproximately(1.0 / 60.0, 1e-9);
    }

    [Fact]
    public void Constructor_DefaultDeltaTime_Is60Hz()
    {
        var clock = new SimulationClock(ClockMode.Stepped);

        clock.DeltaTime.Should().BeApproximately(1.0 / 60.0, 1e-9);
    }

    [Fact]
    public void Constructor_InvalidDeltaTime_Throws()
    {
        var act = () => new SimulationClock(ClockMode.Stepped, deltaTime: 0.0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeDeltaTime_Throws()
    {
        var act = () => new SimulationClock(ClockMode.Stepped, deltaTime: -0.01);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Mode_ReturnsConfiguredMode()
    {
        var clock = new SimulationClock(ClockMode.Accelerated);
        clock.Mode.Should().Be(ClockMode.Accelerated);
    }

    [Fact]
    public void InitialState_IsNotPaused()
    {
        var clock = new SimulationClock(ClockMode.Stepped);
        clock.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void InitialState_ElapsedTimeIsZero()
    {
        var clock = new SimulationClock(ClockMode.Stepped);
        clock.ElapsedTime.Should().Be(0.0);
    }

    [Fact]
    public void Accelerated_AdvanceUsesAccelerationFactor()
    {
        var clock = new SimulationClock(ClockMode.Accelerated, deltaTime: 1.0 / 60.0, accelerationFactor: 10.0);

        clock.Advance();

        clock.ElapsedTime.Should().BeApproximately(10.0 / 60.0, 1e-9);
    }

    [Fact]
    public void Accelerated_EffectiveDeltaTimeReflectsFactor()
    {
        var clock = new SimulationClock(ClockMode.Accelerated, deltaTime: 1.0 / 60.0, accelerationFactor: 100.0);

        clock.EffectiveDeltaTime.Should().BeApproximately(100.0 / 60.0, 1e-9);
    }

    [Fact]
    public void Stepped_EffectiveDeltaTimeIgnoresFactor()
    {
        var clock = new SimulationClock(ClockMode.Stepped, deltaTime: 1.0 / 60.0, accelerationFactor: 100.0);

        clock.EffectiveDeltaTime.Should().BeApproximately(1.0 / 60.0, 1e-9);
    }

    [Fact]
    public void Constructor_InvalidAccelerationFactor_Throws()
    {
        var act = () => new SimulationClock(ClockMode.Accelerated, accelerationFactor: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~SimulationClockTests" -v minimal`
Expected: Build fails — `SimulationClock` class does not exist

- [ ] **Step 4: Implement SimulationClock**

Create `ResQ.Simulation.Engine/Core/SimulationClock.cs`:

```csharp
// (license header)
namespace ResQ.Simulation.Engine.Core;

/// <summary>
/// Controls time progression in the simulation with support for real-time,
/// accelerated, and stepped (deterministic) modes.
/// </summary>
public sealed class SimulationClock : ISimulationClock
{
    private const double DefaultDeltaTime = 1.0 / 60.0;

    /// <inheritdoc />
    public double ElapsedTime { get; private set; }

    /// <inheritdoc />
    public double DeltaTime { get; }

    /// <inheritdoc />
    public ClockMode Mode { get; }

    /// <inheritdoc />
    public bool IsPaused { get; private set; }

    /// <summary>Acceleration factor (only used in Accelerated mode).</summary>
    public double AccelerationFactor { get; }

    /// <summary>
    /// Initializes a new simulation clock.
    /// </summary>
    /// <param name="mode">The clock mode.</param>
    /// <param name="deltaTime">Fixed timestep in seconds. Defaults to 1/60.</param>
    /// <param name="accelerationFactor">Speed multiplier for Accelerated mode. Default: 1.0.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when deltaTime is not positive.</exception>
    public SimulationClock(ClockMode mode, double deltaTime = DefaultDeltaTime, double accelerationFactor = 1.0)
    {
        if (deltaTime <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime,
                "Delta time must be positive");

        if (accelerationFactor <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(accelerationFactor), accelerationFactor,
                "Acceleration factor must be positive");

        Mode = mode;
        DeltaTime = deltaTime;
        AccelerationFactor = accelerationFactor;
    }

    /// <summary>
    /// Returns the effective timestep: DeltaTime * AccelerationFactor in Accelerated mode,
    /// DeltaTime otherwise.
    /// </summary>
    public double EffectiveDeltaTime => Mode == ClockMode.Accelerated
        ? DeltaTime * AccelerationFactor
        : DeltaTime;

    /// <inheritdoc />
    public void Advance()
    {
        if (IsPaused) return;
        ElapsedTime += EffectiveDeltaTime;
    }

    /// <inheritdoc />
    public void Pause() => IsPaused = true;

    /// <inheritdoc />
    public void Resume() => IsPaused = false;
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~SimulationClockTests" -v minimal`
Expected: All 14 tests pass

- [ ] **Step 6: Commit**

```bash
git add ResQ.Simulation.Engine/Core/ tests/ResQ.Simulation.Engine.Tests/Core/SimulationClockTests.cs
git commit -m "feat(sim-engine): implement ISimulationClock with stepped/realtime/accelerated modes"
```

---

### Task 3: Implement SimulationConfig

**Files:**
- Create: `ResQ.Simulation.Engine/Core/SimulationConfig.cs`

- [ ] **Step 1: Create the config class**

Create `ResQ.Simulation.Engine/Core/SimulationConfig.cs`:

```csharp
// (license header)
namespace ResQ.Simulation.Engine.Core;

/// <summary>
/// Configuration for a simulation session. Used with <c>IOptions&lt;SimulationConfig&gt;</c>.
/// </summary>
public sealed class SimulationConfig
{
    /// <summary>Fixed timestep in seconds. Default: 1/60.</summary>
    public double DeltaTime { get; set; } = 1.0 / 60.0;

    /// <summary>Random seed for deterministic simulation. Default: 42.</summary>
    public int Seed { get; set; } = 42;

    /// <summary>Clock mode. Default: Stepped.</summary>
    public ClockMode ClockMode { get; set; } = ClockMode.Stepped;

    /// <summary>Acceleration factor when ClockMode is Accelerated. Default: 1.0.</summary>
    public double AccelerationFactor { get; set; } = 1.0;

    /// <summary>Flight model to use. Default: Kinematic.</summary>
    public FlightModelType FlightModel { get; set; } = FlightModelType.Kinematic;
}

/// <summary>
/// Available flight model implementations.
/// </summary>
public enum FlightModelType
{
    /// <summary>Fast point-to-point with speed/turn constraints.</summary>
    Kinematic,

    /// <summary>Full 6DOF quadrotor dynamics.</summary>
    Quadrotor
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build ResQ.Simulation.Engine/ -c Release`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add ResQ.Simulation.Engine/Core/SimulationConfig.cs
git commit -m "feat(sim-engine): add SimulationConfig with IOptions<T> pattern"
```

---

## Chunk 2: Physics — Flight Models

### Task 4: Implement DronePhysicsState and FlightCommand

**Files:**
- Create: `ResQ.Simulation.Engine/Physics/DronePhysicsState.cs`
- Create: `ResQ.Simulation.Engine/Physics/FlightCommand.cs`
- Create: `ResQ.Simulation.Engine/Physics/IFlightModel.cs`

- [ ] **Step 1: Create DronePhysicsState record**

Create `ResQ.Simulation.Engine/Physics/DronePhysicsState.cs`:

```csharp
// (license header)
using System.Numerics;

namespace ResQ.Simulation.Engine.Physics;

/// <summary>
/// Immutable snapshot of a drone's physical state.
/// </summary>
/// <param name="Position">World position in meters (X=East, Y=Up, Z=North).</param>
/// <param name="Velocity">Linear velocity in m/s.</param>
/// <param name="Orientation">Orientation as a quaternion.</param>
/// <param name="AngularVelocity">Angular velocity in rad/s.</param>
/// <param name="BatteryPercent">Remaining battery 0-100.</param>
public readonly record struct DronePhysicsState(
    Vector3 Position,
    Vector3 Velocity,
    Quaternion Orientation,
    Vector3 AngularVelocity,
    double BatteryPercent
)
{
    /// <summary>
    /// Creates a default state at the given position with full battery.
    /// </summary>
    public static DronePhysicsState AtPosition(Vector3 position) =>
        new(position, Vector3.Zero, Quaternion.Identity, Vector3.Zero, 100.0);
}
```

- [ ] **Step 2: Create FlightCommand**

Create `ResQ.Simulation.Engine/Physics/FlightCommand.cs`:

```csharp
// (license header)
using System.Numerics;

namespace ResQ.Simulation.Engine.Physics;

/// <summary>
/// Types of flight commands a drone can receive.
/// </summary>
public enum FlightCommandType
{
    /// <summary>Hold current position.</summary>
    Hover,

    /// <summary>Fly to a waypoint.</summary>
    GoToWaypoint,

    /// <summary>Return to launch position.</summary>
    ReturnToLaunch,

    /// <summary>Land at current position.</summary>
    Land
}

/// <summary>
/// A command issued to a drone's flight model.
/// </summary>
/// <param name="Type">The command type.</param>
/// <param name="TargetPosition">Target position for waypoint commands. Null for Hover/Land.</param>
/// <param name="DesiredSpeed">Desired speed in m/s. Null uses default.</param>
public readonly record struct FlightCommand(
    FlightCommandType Type,
    Vector3? TargetPosition = null,
    double? DesiredSpeed = null
)
{
    /// <summary>Creates a hover command.</summary>
    public static FlightCommand Hover() => new(FlightCommandType.Hover);

    /// <summary>Creates a waypoint command.</summary>
    public static FlightCommand GoTo(Vector3 target, double? speed = null) =>
        new(FlightCommandType.GoToWaypoint, target, speed);

    /// <summary>Creates a return-to-launch command.</summary>
    public static FlightCommand RTL() => new(FlightCommandType.ReturnToLaunch);

    /// <summary>Creates a land command.</summary>
    public static FlightCommand Land() => new(FlightCommandType.Land);
}
```

- [ ] **Step 3: Create IFlightModel interface**

Create `ResQ.Simulation.Engine/Physics/IFlightModel.cs`:

```csharp
// (license header)
using System.Numerics;

namespace ResQ.Simulation.Engine.Physics;

/// <summary>
/// Interface for drone flight physics models.
/// </summary>
public interface IFlightModel
{
    /// <summary>Current physical state of the drone.</summary>
    DronePhysicsState State { get; }

    /// <summary>The launch position (for RTL).</summary>
    Vector3 LaunchPosition { get; }

    /// <summary>True when the drone has landed.</summary>
    bool HasLanded { get; }

    /// <summary>Applies a flight command to the model.</summary>
    /// <param name="command">The command to execute.</param>
    void ApplyCommand(FlightCommand command);

    /// <summary>Advances the physics simulation by one timestep.</summary>
    /// <param name="dt">Timestep in seconds.</param>
    /// <param name="wind">Wind force vector at the drone's position.</param>
    void Step(double dt, Vector3 wind);
}
```

- [ ] **Step 4: Verify build**

Run: `dotnet build ResQ.Simulation.Engine/ -c Release`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add ResQ.Simulation.Engine/Physics/
git commit -m "feat(sim-engine): add DronePhysicsState, FlightCommand, and IFlightModel interface"
```

---

### Task 5: Implement KinematicFlightModel

**Files:**
- Create: `ResQ.Simulation.Engine/Physics/KinematicFlightModel.cs`
- Create: `tests/ResQ.Simulation.Engine.Tests/Physics/KinematicFlightModelTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/ResQ.Simulation.Engine.Tests/Physics/KinematicFlightModelTests.cs`:

```csharp
// (license header)
using System.Numerics;
using FluentAssertions;
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Physics;

public class KinematicFlightModelTests
{
    private static KinematicFlightModel CreateModel(Vector3? startPos = null)
    {
        var pos = startPos ?? new Vector3(0, 50, 0);
        return new KinematicFlightModel(pos);
    }

    [Fact]
    public void Constructor_SetsInitialState()
    {
        var pos = new Vector3(100, 50, 200);
        var model = new KinematicFlightModel(pos);

        model.State.Position.Should().Be(pos);
        model.State.Velocity.Should().Be(Vector3.Zero);
        model.State.BatteryPercent.Should().Be(100.0);
        model.LaunchPosition.Should().Be(pos);
        model.HasLanded.Should().BeFalse();
    }

    [Fact]
    public void Hover_DroneStaysInPlace()
    {
        var model = CreateModel();
        var initialPos = model.State.Position;

        model.ApplyCommand(FlightCommand.Hover());
        model.Step(1.0, Vector3.Zero);

        model.State.Position.Should().Be(initialPos);
    }

    [Fact]
    public void GoToWaypoint_DroneMovesTowardTarget()
    {
        var model = CreateModel(new Vector3(0, 50, 0));
        var target = new Vector3(100, 50, 0);

        model.ApplyCommand(FlightCommand.GoTo(target));
        model.Step(1.0, Vector3.Zero);

        model.State.Position.X.Should().BeGreaterThan(0);
        model.State.Position.X.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void GoToWaypoint_ReachesTarget_StopsMoving()
    {
        var model = CreateModel(new Vector3(0, 50, 0));
        var target = new Vector3(1, 50, 0); // Very close

        model.ApplyCommand(FlightCommand.GoTo(target, speed: 10.0));

        // Step enough to overshoot
        for (int i = 0; i < 60; i++)
            model.Step(1.0 / 60.0, Vector3.Zero);

        // Should be at or very near target, not overshooting
        var distance = Vector3.Distance(model.State.Position, target);
        distance.Should().BeLessThan(1.0f);
    }

    [Fact]
    public void Wind_OffsetsPosition()
    {
        var model = CreateModel(new Vector3(0, 50, 0));
        model.ApplyCommand(FlightCommand.Hover());

        var wind = new Vector3(10, 0, 0); // 10 m/s east wind
        model.Step(1.0, wind);

        // Kinematic model applies wind as velocity offset
        model.State.Position.X.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BatteryDrains_OverTime()
    {
        var model = CreateModel();
        model.ApplyCommand(FlightCommand.Hover());

        model.Step(1.0, Vector3.Zero);

        model.State.BatteryPercent.Should().BeLessThan(100.0);
    }

    [Fact]
    public void Land_DecreaseAltitude()
    {
        var model = CreateModel(new Vector3(0, 50, 0));
        model.ApplyCommand(FlightCommand.Land());

        model.Step(1.0, Vector3.Zero);

        model.State.Position.Y.Should().BeLessThan(50);
    }

    [Fact]
    public void Land_WhenOnGround_SetsHasLanded()
    {
        var model = CreateModel(new Vector3(0, 1, 0)); // Very low

        model.ApplyCommand(FlightCommand.Land());

        // Step many times to ensure landing
        for (int i = 0; i < 120; i++)
            model.Step(1.0 / 60.0, Vector3.Zero);

        model.HasLanded.Should().BeTrue();
    }

    [Fact]
    public void RTL_MovesTowardLaunchPosition()
    {
        var launchPos = new Vector3(0, 50, 0);
        var model = CreateModel(launchPos);

        // Move away first
        model.ApplyCommand(FlightCommand.GoTo(new Vector3(100, 50, 0)));
        for (int i = 0; i < 300; i++)
            model.Step(1.0 / 60.0, Vector3.Zero);

        // Now RTL
        var posBeforeRtl = model.State.Position;
        model.ApplyCommand(FlightCommand.RTL());
        for (int i = 0; i < 60; i++)
            model.Step(1.0 / 60.0, Vector3.Zero);

        var distBefore = Vector3.Distance(posBeforeRtl, launchPos);
        var distAfter = Vector3.Distance(model.State.Position, launchPos);
        distAfter.Should().BeLessThan(distBefore);
    }

    [Fact]
    public void MaxSpeed_IsRespected()
    {
        var model = CreateModel(new Vector3(0, 50, 0));
        var farTarget = new Vector3(10000, 50, 0);

        model.ApplyCommand(FlightCommand.GoTo(farTarget));
        model.Step(1.0 / 60.0, Vector3.Zero);

        // Velocity magnitude should not exceed max speed (default 15 m/s)
        model.State.Velocity.Length().Should().BeLessThanOrEqualTo(15.1f); // small float tolerance
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~KinematicFlightModelTests" -v minimal`
Expected: Build fails — `KinematicFlightModel` does not exist

- [ ] **Step 3: Implement KinematicFlightModel**

Create `ResQ.Simulation.Engine/Physics/KinematicFlightModel.cs`:

```csharp
// (license header)
using System.Numerics;

namespace ResQ.Simulation.Engine.Physics;

/// <summary>
/// Simple kinematic flight model using position/velocity/acceleration constraints.
/// Suitable for large-scale simulations (10K+ drones).
/// </summary>
public sealed class KinematicFlightModel : IFlightModel
{
    private const double DefaultMaxSpeed = 15.0;         // m/s
    private const double DefaultLandingSpeed = 2.0;      // m/s descent rate
    private const double DefaultBatteryDrainPerSec = 0.1; // % per second
    private const double LandedAltitudeThreshold = 0.5;  // meters
    private const double WaypointReachedThreshold = 1.0; // meters

    private DronePhysicsState _state;
    private FlightCommand _activeCommand;

    /// <inheritdoc />
    public DronePhysicsState State => _state;

    /// <inheritdoc />
    public Vector3 LaunchPosition { get; }

    /// <inheritdoc />
    public bool HasLanded { get; private set; }

    /// <summary>Maximum speed in m/s.</summary>
    public double MaxSpeed { get; }

    /// <summary>
    /// Initializes a new kinematic flight model at the given position.
    /// </summary>
    /// <param name="startPosition">Initial world position (Y = altitude).</param>
    /// <param name="maxSpeed">Maximum speed in m/s. Default: 15.</param>
    public KinematicFlightModel(Vector3 startPosition, double maxSpeed = DefaultMaxSpeed)
    {
        _state = DronePhysicsState.AtPosition(startPosition);
        LaunchPosition = startPosition;
        MaxSpeed = maxSpeed;
        _activeCommand = FlightCommand.Hover();
    }

    /// <inheritdoc />
    public void ApplyCommand(FlightCommand command)
    {
        _activeCommand = command;

        // RTL targets the launch position
        if (command.Type == FlightCommandType.ReturnToLaunch)
        {
            _activeCommand = FlightCommand.GoTo(LaunchPosition, command.DesiredSpeed);
        }
    }

    /// <inheritdoc />
    public void Step(double dt, Vector3 wind)
    {
        if (HasLanded) return;

        var velocity = ComputeVelocity(dt);

        // Apply wind as velocity offset
        var windOffset = wind * (float)dt;

        var newPosition = _state.Position + velocity * (float)dt + windOffset;

        // Clamp altitude to ground
        if (newPosition.Y < 0) newPosition = newPosition with { Y = 0 };

        // Drain battery
        var newBattery = _state.BatteryPercent - DefaultBatteryDrainPerSec * dt;
        if (newBattery < 0) newBattery = 0;

        _state = _state with
        {
            Position = newPosition,
            Velocity = velocity,
            BatteryPercent = newBattery
        };

        // Check landing
        if (_activeCommand.Type == FlightCommandType.Land && newPosition.Y <= LandedAltitudeThreshold)
        {
            _state = _state with { Position = _state.Position with { Y = 0 }, Velocity = Vector3.Zero };
            HasLanded = true;
        }
    }

    private Vector3 ComputeVelocity(double dt)
    {
        switch (_activeCommand.Type)
        {
            case FlightCommandType.Hover:
                return Vector3.Zero;

            case FlightCommandType.GoToWaypoint:
            {
                var target = _activeCommand.TargetPosition ?? _state.Position;
                var toTarget = target - _state.Position;
                var distance = toTarget.Length();

                if (distance < WaypointReachedThreshold)
                    return Vector3.Zero;

                var speed = _activeCommand.DesiredSpeed ?? MaxSpeed;
                speed = Math.Min(speed, MaxSpeed);

                var direction = Vector3.Normalize(toTarget);
                return direction * (float)speed;
            }

            case FlightCommandType.Land:
                return new Vector3(0, -(float)DefaultLandingSpeed, 0);

            default:
                return Vector3.Zero;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~KinematicFlightModelTests" -v minimal`
Expected: All 10 tests pass

- [ ] **Step 5: Commit**

```bash
git add ResQ.Simulation.Engine/Physics/KinematicFlightModel.cs tests/ResQ.Simulation.Engine.Tests/Physics/KinematicFlightModelTests.cs
git commit -m "feat(sim-engine): implement KinematicFlightModel with waypoint navigation and landing"
```

---

### Task 6: Implement QuadrotorFlightModel

**Files:**
- Create: `ResQ.Simulation.Engine/Physics/QuadrotorFlightModel.cs`
- Create: `tests/ResQ.Simulation.Engine.Tests/Physics/QuadrotorFlightModelTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/ResQ.Simulation.Engine.Tests/Physics/QuadrotorFlightModelTests.cs`:

```csharp
// (license header)
using System.Numerics;
using FluentAssertions;
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Physics;

public class QuadrotorFlightModelTests
{
    private static QuadrotorFlightModel CreateModel(Vector3? startPos = null)
    {
        var pos = startPos ?? new Vector3(0, 50, 0);
        return new QuadrotorFlightModel(pos, mass: 2.5);
    }

    [Fact]
    public void Constructor_SetsInitialState()
    {
        var pos = new Vector3(100, 50, 200);
        var model = new QuadrotorFlightModel(pos, mass: 2.5);

        model.State.Position.Should().Be(pos);
        model.State.Velocity.Should().Be(Vector3.Zero);
        model.State.BatteryPercent.Should().Be(100.0);
        model.HasLanded.Should().BeFalse();
    }

    [Fact]
    public void Hover_MaintainsAltitude_WithinTolerance()
    {
        var model = CreateModel(new Vector3(0, 50, 0));
        model.ApplyCommand(FlightCommand.Hover());

        // Run for 2 seconds
        for (int i = 0; i < 120; i++)
            model.Step(1.0 / 60.0, Vector3.Zero);

        // Should be near starting altitude (gravity balanced by thrust)
        model.State.Position.Y.Should().BeApproximately(50f, 5f);
    }

    [Fact]
    public void NoThrust_FallsDueToGravity()
    {
        var model = CreateModel(new Vector3(0, 50, 0));
        // Don't apply any command — no thrust

        // Manually step without thrust (land command descends)
        model.ApplyCommand(FlightCommand.Land());
        model.Step(1.0, Vector3.Zero);

        model.State.Position.Y.Should().BeLessThan(50);
    }

    [Fact]
    public void Wind_AffectsPosition()
    {
        var model = CreateModel(new Vector3(0, 50, 0));
        model.ApplyCommand(FlightCommand.Hover());

        var strongWind = new Vector3(20, 0, 0);

        for (int i = 0; i < 60; i++)
            model.Step(1.0 / 60.0, strongWind);

        // Drone should drift east due to wind
        model.State.Position.X.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GoToWaypoint_MovesTowardTarget()
    {
        var model = CreateModel(new Vector3(0, 50, 0));
        model.ApplyCommand(FlightCommand.GoTo(new Vector3(50, 50, 0)));

        for (int i = 0; i < 300; i++)
            model.Step(1.0 / 60.0, Vector3.Zero);

        model.State.Position.X.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BatteryDrains_ProportionalToThrust()
    {
        var model = CreateModel();
        model.ApplyCommand(FlightCommand.Hover());

        for (int i = 0; i < 60; i++)
            model.Step(1.0 / 60.0, Vector3.Zero);

        model.State.BatteryPercent.Should().BeLessThan(100.0);
    }

    [Fact]
    public void Drag_LimitsMaxVelocity()
    {
        var model = CreateModel(new Vector3(0, 50, 0));
        model.ApplyCommand(FlightCommand.GoTo(new Vector3(10000, 50, 0)));

        // Run for a while to approach terminal velocity
        for (int i = 0; i < 600; i++)
            model.Step(1.0 / 60.0, Vector3.Zero);

        // Velocity should be bounded (not increase indefinitely)
        model.State.Velocity.Length().Should().BeLessThan(30f); // reasonable upper bound
    }

    [Fact]
    public void Constructor_InvalidMass_Throws()
    {
        var act = () => new QuadrotorFlightModel(Vector3.Zero, mass: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~QuadrotorFlightModelTests" -v minimal`
Expected: Build fails — `QuadrotorFlightModel` does not exist

- [ ] **Step 3: Implement QuadrotorFlightModel**

Create `ResQ.Simulation.Engine/Physics/QuadrotorFlightModel.cs`:

```csharp
// (license header)
using System.Numerics;

namespace ResQ.Simulation.Engine.Physics;

/// <summary>
/// 6DOF quadrotor flight model with thrust, drag, gravity, and wind.
/// Suitable for high-fidelity simulations with small drone counts.
/// </summary>
public sealed class QuadrotorFlightModel : IFlightModel
{
    private const double Gravity = 9.81;                    // m/s^2
    private const double DragCoefficient = 0.5;             // Simplified drag
    private const double MaxThrustPerRotor = 15.0;          // Newtons
    private const double NumRotors = 4;
    private const double LandingDescentSpeed = 2.0;         // m/s
    private const double LandedAltitudeThreshold = 0.5;     // meters
    private const double WaypointReachedThreshold = 2.0;    // meters
    private const double BatteryDrainBase = 0.02;           // % per second base
    private const double BatteryDrainThrustFactor = 0.005;  // % per Newton per second
    private const double PidGainP = 4.0;                    // Proportional gain for position control
    private const double PidGainD = 3.0;                    // Derivative gain (damping)

    private DronePhysicsState _state;
    private FlightCommand _activeCommand;
    private readonly double _mass;

    /// <inheritdoc />
    public DronePhysicsState State => _state;

    /// <inheritdoc />
    public Vector3 LaunchPosition { get; }

    /// <inheritdoc />
    public bool HasLanded { get; private set; }

    /// <summary>
    /// Initializes a new quadrotor flight model.
    /// </summary>
    /// <param name="startPosition">Initial world position (Y = altitude).</param>
    /// <param name="mass">Drone mass in kg.</param>
    public QuadrotorFlightModel(Vector3 startPosition, double mass)
    {
        if (mass <= 0)
            throw new ArgumentOutOfRangeException(nameof(mass), mass,
                "Mass must be positive");

        _state = DronePhysicsState.AtPosition(startPosition);
        LaunchPosition = startPosition;
        _mass = mass;
        _activeCommand = FlightCommand.Hover();
    }

    /// <inheritdoc />
    public void ApplyCommand(FlightCommand command)
    {
        _activeCommand = command;

        if (command.Type == FlightCommandType.ReturnToLaunch)
        {
            _activeCommand = FlightCommand.GoTo(LaunchPosition, command.DesiredSpeed);
        }
    }

    /// <inheritdoc />
    public void Step(double dt, Vector3 wind)
    {
        if (HasLanded) return;

        // Compute desired thrust based on command
        var thrustForce = ComputeThrust(dt);

        // Gravity
        var gravityForce = new Vector3(0, -(float)(_mass * Gravity), 0);

        // Drag: F_drag = -dragCoeff * v^2 * direction
        var speed = _state.Velocity.Length();
        var dragForce = speed > 0.001f
            ? -Vector3.Normalize(_state.Velocity) * (float)(DragCoefficient * speed * speed)
            : Vector3.Zero;

        // Wind force
        var windForce = wind * (float)_mass * 0.1f; // Wind effect scaled by mass

        // Net force
        var netForce = thrustForce + gravityForce + dragForce + windForce;

        // Acceleration (F = ma)
        var acceleration = netForce / (float)_mass;

        // Integrate velocity and position (semi-implicit Euler)
        var newVelocity = _state.Velocity + acceleration * (float)dt;
        var newPosition = _state.Position + newVelocity * (float)dt;

        // Ground collision
        if (newPosition.Y < 0)
        {
            newPosition = newPosition with { Y = 0 };
            newVelocity = newVelocity with { Y = Math.Max(0, newVelocity.Y) };
        }

        // Battery drain proportional to thrust magnitude
        var thrustMagnitude = thrustForce.Length();
        var batteryDrain = (BatteryDrainBase + BatteryDrainThrustFactor * thrustMagnitude) * dt;
        var newBattery = Math.Max(0, _state.BatteryPercent - batteryDrain);

        _state = _state with
        {
            Position = newPosition,
            Velocity = newVelocity,
            BatteryPercent = newBattery
        };

        // Check landing
        if (_activeCommand.Type == FlightCommandType.Land && newPosition.Y <= LandedAltitudeThreshold)
        {
            _state = _state with { Position = _state.Position with { Y = 0 }, Velocity = Vector3.Zero };
            HasLanded = true;
        }
    }

    private Vector3 ComputeThrust(double dt)
    {
        var maxThrust = (float)(MaxThrustPerRotor * NumRotors);

        switch (_activeCommand.Type)
        {
            case FlightCommandType.Hover:
            {
                // PD controller to maintain position
                var hoverThrust = (float)(_mass * Gravity); // Counteract gravity
                var verticalCorrection = -_state.Velocity.Y * (float)PidGainD;
                return new Vector3(0, hoverThrust + verticalCorrection, 0);
            }

            case FlightCommandType.GoToWaypoint:
            {
                var target = _activeCommand.TargetPosition ?? _state.Position;
                var error = target - _state.Position;
                var distance = error.Length();

                if (distance < WaypointReachedThreshold)
                {
                    // Switch to hover behavior
                    var hoverThrust = (float)(_mass * Gravity);
                    var damping = -_state.Velocity * (float)PidGainD;
                    return new Vector3(damping.X, hoverThrust + damping.Y, damping.Z);
                }

                // PD controller: thrust = P * error - D * velocity + gravity compensation
                var pTerm = error * (float)PidGainP;
                var dTerm = -_state.Velocity * (float)PidGainD;
                var gravComp = new Vector3(0, (float)(_mass * Gravity), 0);

                var thrust = pTerm + dTerm + gravComp;

                // Clamp to max thrust
                if (thrust.Length() > maxThrust)
                    thrust = Vector3.Normalize(thrust) * maxThrust;

                return thrust;
            }

            case FlightCommandType.Land:
            {
                // Controlled descent: just enough thrust for a slow descent
                var desiredVertVelocity = -(float)LandingDescentSpeed;
                var vertError = desiredVertVelocity - _state.Velocity.Y;
                var gravComp = (float)(_mass * Gravity);
                var correction = vertError * (float)PidGainD;
                return new Vector3(0, gravComp + correction, 0);
            }

            default:
                return Vector3.Zero;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~QuadrotorFlightModelTests" -v minimal`
Expected: All 7 tests pass

- [ ] **Step 5: Commit**

```bash
git add ResQ.Simulation.Engine/Physics/QuadrotorFlightModel.cs tests/ResQ.Simulation.Engine.Tests/Physics/QuadrotorFlightModelTests.cs
git commit -m "feat(sim-engine): implement QuadrotorFlightModel with 6DOF dynamics and PD control"
```

---

## Chunk 3: Environment — Terrain + Weather

### Task 7: Implement Terrain

**Files:**
- Create: `ResQ.Simulation.Engine/Environment/SurfaceType.cs`
- Create: `ResQ.Simulation.Engine/Environment/ITerrain.cs`
- Create: `ResQ.Simulation.Engine/Environment/FlatTerrain.cs`
- Create: `ResQ.Simulation.Engine/Environment/HeightmapTerrain.cs`
- Create: `tests/ResQ.Simulation.Engine.Tests/Environment/HeightmapTerrainTests.cs`

- [ ] **Step 1: Create SurfaceType enum and ITerrain interface**

Create `ResQ.Simulation.Engine/Environment/SurfaceType.cs`:

```csharp
// (license header)
namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// Types of terrain surface, affecting hazard spread rates and drone detection.
/// </summary>
public enum SurfaceType
{
    /// <summary>Trees, grass, shrubs — high fire spread rate.</summary>
    Vegetation,

    /// <summary>Water body — blocks fire, contributes to flooding.</summary>
    Water,

    /// <summary>Buildings, roads, concrete — moderate fire spread.</summary>
    Urban,

    /// <summary>Exposed ground, rock — low fire spread.</summary>
    BareGround
}
```

Create `ResQ.Simulation.Engine/Environment/ITerrain.cs`:

```csharp
// (license header)
namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// Provides terrain elevation and surface type queries.
/// </summary>
public interface ITerrain
{
    /// <summary>Returns elevation in meters above sea level at the given world coordinates.</summary>
    /// <param name="x">East-West position in meters.</param>
    /// <param name="z">North-South position in meters.</param>
    double GetElevation(double x, double z);

    /// <summary>Returns the surface type at the given world coordinates.</summary>
    /// <param name="x">East-West position in meters.</param>
    /// <param name="z">North-South position in meters.</param>
    SurfaceType GetSurfaceType(double x, double z);

    /// <summary>Width of the terrain in meters (X axis).</summary>
    double Width { get; }

    /// <summary>Depth of the terrain in meters (Z axis).</summary>
    double Depth { get; }
}
```

- [ ] **Step 2: Create FlatTerrain**

Create `ResQ.Simulation.Engine/Environment/FlatTerrain.cs`:

```csharp
// (license header)
namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// A flat terrain at a uniform elevation. Useful for testing and simple scenarios.
/// </summary>
public sealed class FlatTerrain : ITerrain
{
    private readonly double _elevation;
    private readonly SurfaceType _surfaceType;

    /// <inheritdoc />
    public double Width { get; }

    /// <inheritdoc />
    public double Depth { get; }

    /// <summary>
    /// Creates a flat terrain.
    /// </summary>
    /// <param name="width">Width in meters.</param>
    /// <param name="depth">Depth in meters.</param>
    /// <param name="elevation">Uniform elevation in meters. Default: 0.</param>
    /// <param name="surfaceType">Uniform surface type. Default: Vegetation.</param>
    public FlatTerrain(double width = 1000, double depth = 1000,
        double elevation = 0, SurfaceType surfaceType = SurfaceType.Vegetation)
    {
        Width = width;
        Depth = depth;
        _elevation = elevation;
        _surfaceType = surfaceType;
    }

    /// <inheritdoc />
    public double GetElevation(double x, double z) => _elevation;

    /// <inheritdoc />
    public SurfaceType GetSurfaceType(double x, double z) => _surfaceType;
}
```

- [ ] **Step 3: Write failing tests for HeightmapTerrain**

Create `tests/ResQ.Simulation.Engine.Tests/Environment/HeightmapTerrainTests.cs`:

```csharp
// (license header)
using FluentAssertions;
using ResQ.Simulation.Engine.Environment;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Environment;

public class HeightmapTerrainTests
{
    [Fact]
    public void GetElevation_ReturnsInterpolatedHeight()
    {
        // 2x2 heightmap: corners at 0, 10, 20, 30 meters
        var heights = new float[,]
        {
            { 0f, 10f },
            { 20f, 30f }
        };

        var terrain = new HeightmapTerrain(heights, width: 100, depth: 100);

        // Center should interpolate to ~15
        var center = terrain.GetElevation(50, 50);
        center.Should().BeApproximately(15.0, 1.0);
    }

    [Fact]
    public void GetElevation_AtCorner_ReturnsExactHeight()
    {
        var heights = new float[,]
        {
            { 0f, 10f },
            { 20f, 30f }
        };

        var terrain = new HeightmapTerrain(heights, width: 100, depth: 100);

        terrain.GetElevation(0, 0).Should().BeApproximately(0.0, 0.1);
    }

    [Fact]
    public void GetElevation_OutOfBounds_ClampsToEdge()
    {
        var heights = new float[,]
        {
            { 5f, 5f },
            { 5f, 5f }
        };

        var terrain = new HeightmapTerrain(heights, width: 100, depth: 100);

        terrain.GetElevation(-50, -50).Should().BeApproximately(5.0, 0.1);
        terrain.GetElevation(200, 200).Should().BeApproximately(5.0, 0.1);
    }

    [Fact]
    public void Constructor_EmptyHeightmap_Throws()
    {
        var act = () => new HeightmapTerrain(new float[0, 0], width: 100, depth: 100);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_InvalidDimensions_Throws()
    {
        var heights = new float[,] { { 1f } };
        var act = () => new HeightmapTerrain(heights, width: 0, depth: 100);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetSurfaceType_DefaultIsVegetation()
    {
        var heights = new float[,] { { 0f, 0f }, { 0f, 0f } };
        var terrain = new HeightmapTerrain(heights, width: 100, depth: 100);

        terrain.GetSurfaceType(50, 50).Should().Be(SurfaceType.Vegetation);
    }

    [Fact]
    public void GetSurfaceType_WithSurfaceMap_ReturnsCorrectType()
    {
        var heights = new float[,] { { 0f, 0f }, { 0f, 0f } };
        var surfaces = new SurfaceType[,]
        {
            { SurfaceType.Water, SurfaceType.Urban },
            { SurfaceType.BareGround, SurfaceType.Vegetation }
        };

        var terrain = new HeightmapTerrain(heights, width: 100, depth: 100, surfaceMap: surfaces);

        terrain.GetSurfaceType(0, 0).Should().Be(SurfaceType.Water);
        terrain.GetSurfaceType(99, 99).Should().Be(SurfaceType.Vegetation);
    }

    [Fact]
    public void Width_And_Depth_ReturnConfiguredValues()
    {
        var heights = new float[,] { { 0f } };
        var terrain = new HeightmapTerrain(heights, width: 500, depth: 300);

        terrain.Width.Should().Be(500);
        terrain.Depth.Should().Be(300);
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~HeightmapTerrainTests" -v minimal`
Expected: Build fails — `HeightmapTerrain` does not exist

- [ ] **Step 5: Implement HeightmapTerrain**

Create `ResQ.Simulation.Engine/Environment/HeightmapTerrain.cs`:

```csharp
// (license header)
namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// Heightmap-based terrain with bilinear interpolation for elevation queries.
/// </summary>
public sealed class HeightmapTerrain : ITerrain
{
    private readonly float[,] _heights;
    private readonly SurfaceType[,]? _surfaces;
    private readonly int _rows;
    private readonly int _cols;

    /// <inheritdoc />
    public double Width { get; }

    /// <inheritdoc />
    public double Depth { get; }

    /// <summary>
    /// Creates a heightmap terrain.
    /// </summary>
    /// <param name="heights">2D array of elevation values in meters. [row, col] where row=Z, col=X.</param>
    /// <param name="width">Width in meters (X axis).</param>
    /// <param name="depth">Depth in meters (Z axis).</param>
    /// <param name="surfaceMap">Optional surface type map (same dimensions as heights). Default: all Vegetation.</param>
    public HeightmapTerrain(float[,] heights, double width, double depth, SurfaceType[,]? surfaceMap = null)
    {
        ArgumentNullException.ThrowIfNull(heights, nameof(heights));

        _rows = heights.GetLength(0);
        _cols = heights.GetLength(1);

        if (_rows == 0 || _cols == 0)
            throw new ArgumentException("Heightmap must have at least one element", nameof(heights));

        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive");

        if (depth <= 0)
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "Depth must be positive");

        if (surfaceMap != null &&
            (surfaceMap.GetLength(0) != _rows || surfaceMap.GetLength(1) != _cols))
        {
            throw new ArgumentException("Surface map dimensions must match heightmap", nameof(surfaceMap));
        }

        _heights = heights;
        _surfaces = surfaceMap;
        Width = width;
        Depth = depth;
    }

    /// <inheritdoc />
    public double GetElevation(double x, double z)
    {
        // Map world coords to grid coords
        var gx = x / Width * (_cols - 1);
        var gz = z / Depth * (_rows - 1);

        // Clamp to grid bounds
        gx = Math.Clamp(gx, 0, _cols - 1);
        gz = Math.Clamp(gz, 0, _rows - 1);

        // Bilinear interpolation
        var x0 = (int)Math.Floor(gx);
        var z0 = (int)Math.Floor(gz);
        var x1 = Math.Min(x0 + 1, _cols - 1);
        var z1 = Math.Min(z0 + 1, _rows - 1);

        var fx = gx - x0;
        var fz = gz - z0;

        var h00 = _heights[z0, x0];
        var h10 = _heights[z0, x1];
        var h01 = _heights[z1, x0];
        var h11 = _heights[z1, x1];

        var h0 = h00 + (h10 - h00) * fx;
        var h1 = h01 + (h11 - h01) * fx;

        return h0 + (h1 - h0) * fz;
    }

    /// <inheritdoc />
    public SurfaceType GetSurfaceType(double x, double z)
    {
        if (_surfaces == null)
            return SurfaceType.Vegetation;

        var gx = x / Width * (_cols - 1);
        var gz = z / Depth * (_rows - 1);

        var col = (int)Math.Clamp(Math.Round(gx), 0, _cols - 1);
        var row = (int)Math.Clamp(Math.Round(gz), 0, _rows - 1);

        return _surfaces[row, col];
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~HeightmapTerrainTests" -v minimal`
Expected: All 8 tests pass

- [ ] **Step 7: Commit**

```bash
git add ResQ.Simulation.Engine/Environment/ tests/ResQ.Simulation.Engine.Tests/Environment/HeightmapTerrainTests.cs
git commit -m "feat(sim-engine): implement ITerrain with FlatTerrain and HeightmapTerrain"
```

---

### Task 8: Implement WeatherSystem

**Files:**
- Create: `ResQ.Simulation.Engine/Environment/WeatherConfig.cs`
- Create: `ResQ.Simulation.Engine/Environment/IWeatherSystem.cs`
- Create: `ResQ.Simulation.Engine/Environment/WeatherSystem.cs`
- Create: `tests/ResQ.Simulation.Engine.Tests/Environment/WeatherSystemTests.cs`

- [ ] **Step 1: Create WeatherConfig and IWeatherSystem**

Create `ResQ.Simulation.Engine/Environment/WeatherConfig.cs`:

```csharp
// (license header)
namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// Weather mode for the simulation.
/// </summary>
public enum WeatherMode
{
    /// <summary>No wind, full visibility.</summary>
    Calm,

    /// <summary>Constant wind from a fixed direction.</summary>
    Steady,

    /// <summary>Variable wind with Perlin noise-based gusts.</summary>
    Turbulent
}

/// <summary>
/// Configuration for the weather system.
/// </summary>
public sealed class WeatherConfig
{
    /// <summary>Weather mode. Default: Calm.</summary>
    public WeatherMode Mode { get; set; } = WeatherMode.Calm;

    /// <summary>Wind direction in degrees (0=North, 90=East). Used in Steady/Turbulent modes.</summary>
    public double WindDirection { get; set; }

    /// <summary>Base wind speed in m/s. Used in Steady/Turbulent modes.</summary>
    public double WindSpeed { get; set; }

    /// <summary>Visibility scalar (0.0 = zero visibility, 1.0 = clear). Default: 1.0.</summary>
    public double Visibility { get; set; } = 1.0;

    /// <summary>Precipitation intensity (0.0 = none, 1.0 = heavy). Default: 0.0.</summary>
    public double Precipitation { get; set; }

    /// <summary>Random seed for turbulent mode. Default: 0.</summary>
    public int TurbulenceSeed { get; set; }
}
```

Create `ResQ.Simulation.Engine/Environment/IWeatherSystem.cs`:

```csharp
// (license header)
using System.Numerics;

namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// Provides weather queries and time-varying wind fields.
/// </summary>
public interface IWeatherSystem
{
    /// <summary>Returns the wind force vector at the given world position.</summary>
    /// <param name="x">East-West position.</param>
    /// <param name="y">Altitude.</param>
    /// <param name="z">North-South position.</param>
    Vector3 GetWind(double x, double y, double z);

    /// <summary>Current visibility scalar (0.0 to 1.0).</summary>
    double Visibility { get; }

    /// <summary>Current precipitation intensity (0.0 to 1.0).</summary>
    double Precipitation { get; }

    /// <summary>Advances the weather simulation by one timestep.</summary>
    /// <param name="dt">Timestep in seconds.</param>
    void Step(double dt);
}
```

- [ ] **Step 2: Write failing tests for WeatherSystem**

Create `tests/ResQ.Simulation.Engine.Tests/Environment/WeatherSystemTests.cs`:

```csharp
// (license header)
using System.Numerics;
using FluentAssertions;
using ResQ.Simulation.Engine.Environment;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Environment;

public class WeatherSystemTests
{
    [Fact]
    public void Calm_WindIsZero()
    {
        var config = new WeatherConfig { Mode = WeatherMode.Calm };
        var weather = new WeatherSystem(config);

        var wind = weather.GetWind(0, 50, 0);

        wind.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Steady_WindMatchesConfig()
    {
        var config = new WeatherConfig
        {
            Mode = WeatherMode.Steady,
            WindDirection = 0,   // North
            WindSpeed = 10
        };
        var weather = new WeatherSystem(config);

        var wind = weather.GetWind(0, 50, 0);

        // North wind: positive Z component
        wind.Z.Should().BeGreaterThan(0);
        wind.Length().Should().BeApproximately(10f, 0.5f);
    }

    [Fact]
    public void Steady_EastWind()
    {
        var config = new WeatherConfig
        {
            Mode = WeatherMode.Steady,
            WindDirection = 90,  // East
            WindSpeed = 5
        };
        var weather = new WeatherSystem(config);

        var wind = weather.GetWind(0, 50, 0);

        wind.X.Should().BeGreaterThan(0);
        wind.Length().Should().BeApproximately(5f, 0.5f);
    }

    [Fact]
    public void Turbulent_WindVariesOverTime()
    {
        var config = new WeatherConfig
        {
            Mode = WeatherMode.Turbulent,
            WindDirection = 0,
            WindSpeed = 10,
            TurbulenceSeed = 42
        };
        var weather = new WeatherSystem(config);

        var wind1 = weather.GetWind(0, 50, 0);
        weather.Step(5.0); // Advance time significantly
        var wind2 = weather.GetWind(0, 50, 0);

        // Wind should change over time in turbulent mode
        wind1.Should().NotBe(wind2);
    }

    [Fact]
    public void Turbulent_SameSeed_SameResults()
    {
        var config1 = new WeatherConfig { Mode = WeatherMode.Turbulent, WindSpeed = 10, TurbulenceSeed = 42 };
        var config2 = new WeatherConfig { Mode = WeatherMode.Turbulent, WindSpeed = 10, TurbulenceSeed = 42 };

        var w1 = new WeatherSystem(config1);
        var w2 = new WeatherSystem(config2);

        w1.GetWind(100, 50, 200).Should().Be(w2.GetWind(100, 50, 200));
    }

    [Fact]
    public void Visibility_ReturnsConfiguredValue()
    {
        var config = new WeatherConfig { Mode = WeatherMode.Calm, Visibility = 0.3 };
        var weather = new WeatherSystem(config);

        weather.Visibility.Should().Be(0.3);
    }

    [Fact]
    public void Precipitation_ReturnsConfiguredValue()
    {
        var config = new WeatherConfig { Mode = WeatherMode.Calm, Precipitation = 0.8 };
        var weather = new WeatherSystem(config);

        weather.Precipitation.Should().Be(0.8);
    }

    [Fact]
    public void Wind_IncreasesWithAltitude()
    {
        var config = new WeatherConfig
        {
            Mode = WeatherMode.Steady,
            WindDirection = 90,
            WindSpeed = 10
        };
        var weather = new WeatherSystem(config);

        var windLow = weather.GetWind(0, 10, 0);
        var windHigh = weather.GetWind(0, 100, 0);

        windHigh.Length().Should().BeGreaterThan(windLow.Length());
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~WeatherSystemTests" -v minimal`
Expected: Build fails — `WeatherSystem` does not exist

- [ ] **Step 4: Implement WeatherSystem**

Create `ResQ.Simulation.Engine/Environment/WeatherSystem.cs`:

```csharp
// (license header)
using System.Numerics;

namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// Weather system providing wind fields, visibility, and precipitation.
/// Supports calm, steady, and turbulent (Perlin noise) wind modes.
/// </summary>
public sealed class WeatherSystem : IWeatherSystem
{
    private const double AltitudeWindScale = 0.005; // Wind increases ~0.5% per meter of altitude
    private const double ReferenceAltitude = 50.0;  // Reference altitude for base wind speed

    private readonly WeatherConfig _config;
    private readonly Random _rng;
    private double _time;

    // Precomputed base wind direction as unit vector
    private readonly Vector3 _baseWindDirection;

    /// <inheritdoc />
    public double Visibility => _config.Visibility;

    /// <inheritdoc />
    public double Precipitation => _config.Precipitation;

    /// <summary>
    /// Creates a weather system from configuration.
    /// </summary>
    /// <param name="config">Weather configuration.</param>
    public WeatherSystem(WeatherConfig config)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        _config = config;
        _rng = new Random(config.TurbulenceSeed);

        // Convert direction degrees to unit vector (0=North=+Z, 90=East=+X)
        var radians = config.WindDirection * Math.PI / 180.0;
        _baseWindDirection = new Vector3((float)Math.Sin(radians), 0, (float)Math.Cos(radians));
    }

    /// <inheritdoc />
    public Vector3 GetWind(double x, double y, double z)
    {
        if (_config.Mode == WeatherMode.Calm)
            return Vector3.Zero;

        // Base wind at reference altitude
        var baseWind = _baseWindDirection * (float)_config.WindSpeed;

        // Altitude scaling: wind increases with altitude
        var altFactor = 1.0 + AltitudeWindScale * (y - ReferenceAltitude);
        altFactor = Math.Max(0.5, altFactor); // Floor at 50% of base wind
        baseWind *= (float)altFactor;

        if (_config.Mode == WeatherMode.Steady)
            return baseWind;

        // Turbulent: add noise-based gusts
        var gust = ComputeGust(x, y, z);
        return baseWind + gust;
    }

    /// <inheritdoc />
    public void Step(double dt)
    {
        _time += dt;
    }

    private Vector3 ComputeGust(double x, double y, double z)
    {
        // Simple hash-based pseudo-noise using position + time
        // Not true Perlin but deterministic and varies over space/time
        var gustStrength = (float)(_config.WindSpeed * 0.3); // Gusts up to 30% of base

        var hash1 = HashNoise(x * 0.01 + _time * 0.5, z * 0.01, 0);
        var hash2 = HashNoise(x * 0.01, z * 0.01 + _time * 0.5, 1);
        var hash3 = HashNoise(x * 0.01 + _time * 0.3, z * 0.01 + _time * 0.3, 2);

        return new Vector3(
            (float)(hash1 * gustStrength),
            (float)(hash3 * gustStrength * 0.3), // Less vertical gust
            (float)(hash2 * gustStrength)
        );
    }

    private double HashNoise(double x, double z, int offset)
    {
        // Deterministic hash-based noise in range [-1, 1]
        var ix = (int)(x * 1000) + offset * 73856093;
        var iz = (int)(z * 1000) + offset * 19349663;
        var hash = ix ^ iz;
        hash = (hash * 0x45d9f3b + _config.TurbulenceSeed) & 0x7FFFFFFF;
        return (hash / (double)0x7FFFFFFF) * 2.0 - 1.0;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~WeatherSystemTests" -v minimal`
Expected: All 8 tests pass

- [ ] **Step 6: Commit**

```bash
git add ResQ.Simulation.Engine/Environment/ tests/ResQ.Simulation.Engine.Tests/Environment/WeatherSystemTests.cs
git commit -m "feat(sim-engine): implement IWeatherSystem with calm/steady/turbulent wind modes"
```

---

## Chunk 4: Entities + SimulationWorld

### Task 9: Implement SimulatedDrone entity

**Files:**
- Create: `ResQ.Simulation.Engine/Entities/SimulatedDrone.cs`
- Create: `tests/ResQ.Simulation.Engine.Tests/Entities/SimulatedDroneTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/ResQ.Simulation.Engine.Tests/Entities/SimulatedDroneTests.cs`:

```csharp
// (license header)
using System.Numerics;
using FluentAssertions;
using ResQ.Simulation.Engine.Core;
using ResQ.Simulation.Engine.Entities;
using ResQ.Simulation.Engine.Environment;
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Entities;

public class SimulatedDroneTests
{
    private static SimulatedDrone CreateDrone(
        string id = "test-drone",
        Vector3? position = null,
        FlightModelType modelType = FlightModelType.Kinematic)
    {
        var pos = position ?? new Vector3(0, 50, 0);
        return new SimulatedDrone(id, pos, modelType);
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var drone = CreateDrone("drone-1", new Vector3(10, 50, 20));

        drone.Id.Should().Be("drone-1");
        drone.FlightModel.State.Position.Should().Be(new Vector3(10, 50, 20));
        drone.FlightModel.State.BatteryPercent.Should().Be(100.0);
    }

    [Fact]
    public void Constructor_NullId_Throws()
    {
        var act = () => new SimulatedDrone(null!, Vector3.Zero, FlightModelType.Kinematic);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Step_DelegatestoFlightModel()
    {
        var drone = CreateDrone();
        drone.SendCommand(FlightCommand.GoTo(new Vector3(100, 50, 0)));

        drone.Step(1.0 / 60.0, Vector3.Zero);

        drone.FlightModel.State.Position.X.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Step_WithWeatherWind_AffectsPosition()
    {
        var drone = CreateDrone();
        drone.SendCommand(FlightCommand.Hover());

        drone.Step(1.0, new Vector3(10, 0, 0));

        drone.FlightModel.State.Position.X.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DetectionProbability_AffectedByVisibility()
    {
        var drone = CreateDrone();
        // With full visibility, base detection probability should be higher
        // than with reduced visibility. This is statistical so we just check
        // the method exists and returns reasonable values.
        var probClear = drone.GetDetectionProbability(visibility: 1.0);
        var probFoggy = drone.GetDetectionProbability(visibility: 0.2);

        probClear.Should().BeGreaterThan(probFoggy);
    }

    [Fact]
    public void Kinematic_ModelIsKinematic()
    {
        var drone = CreateDrone(modelType: FlightModelType.Kinematic);
        drone.FlightModel.Should().BeOfType<KinematicFlightModel>();
    }

    [Fact]
    public void Quadrotor_ModelIsQuadrotor()
    {
        var drone = CreateDrone(modelType: FlightModelType.Quadrotor);
        drone.FlightModel.Should().BeOfType<QuadrotorFlightModel>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~SimulatedDroneTests" -v minimal`
Expected: Build fails — `SimulatedDrone` does not exist

- [ ] **Step 3: Implement SimulatedDrone**

Create `ResQ.Simulation.Engine/Entities/SimulatedDrone.cs`:

```csharp
// (license header)
using System.Numerics;
using ResQ.Simulation.Engine.Core;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Simulation.Engine.Entities;

/// <summary>
/// A simulated drone entity that owns a flight model and sensor behavior.
/// </summary>
public sealed class SimulatedDrone
{
    private const double BaseDetectionProbability = 0.05; // 5% per second at full visibility
    private const double DefaultQuadrotorMass = 2.5;      // kg

    /// <summary>Unique identifier for this drone.</summary>
    public string Id { get; }

    /// <summary>The physics model driving this drone.</summary>
    public IFlightModel FlightModel { get; }

    /// <summary>Number of telemetry packets sent.</summary>
    public int TelemetryCount { get; private set; }

    /// <summary>Number of detections made.</summary>
    public int DetectionCount { get; set; }

    /// <summary>
    /// Creates a new simulated drone.
    /// </summary>
    /// <param name="id">Unique drone identifier.</param>
    /// <param name="startPosition">Initial world position.</param>
    /// <param name="modelType">Which flight model to use.</param>
    /// <param name="mass">Drone mass in kg (used by quadrotor model). Default: 2.5.</param>
    public SimulatedDrone(string id, Vector3 startPosition, FlightModelType modelType, double mass = DefaultQuadrotorMass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));

        Id = id;
        FlightModel = modelType switch
        {
            FlightModelType.Kinematic => new KinematicFlightModel(startPosition),
            FlightModelType.Quadrotor => new QuadrotorFlightModel(startPosition, mass),
            _ => throw new ArgumentOutOfRangeException(nameof(modelType))
        };
    }

    /// <summary>
    /// Sends a flight command to this drone.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    public void SendCommand(FlightCommand command)
    {
        FlightModel.ApplyCommand(command);
    }

    /// <summary>
    /// Advances the drone simulation by one timestep.
    /// </summary>
    /// <param name="dt">Timestep in seconds.</param>
    /// <param name="wind">Wind force at the drone's position.</param>
    public void Step(double dt, Vector3 wind)
    {
        FlightModel.Step(dt, wind);
        TelemetryCount++;
    }

    /// <summary>
    /// Returns the probability of detecting a hazard this tick, given visibility.
    /// </summary>
    /// <param name="visibility">Visibility scalar (0.0 to 1.0).</param>
    public double GetDetectionProbability(double visibility)
    {
        return BaseDetectionProbability * Math.Clamp(visibility, 0.0, 1.0);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~SimulatedDroneTests" -v minimal`
Expected: All 7 tests pass

- [ ] **Step 5: Commit**

```bash
git add ResQ.Simulation.Engine/Entities/ tests/ResQ.Simulation.Engine.Tests/Entities/
git commit -m "feat(sim-engine): implement SimulatedDrone entity with flight model and detection"
```

---

### Task 10: Implement Structure entity

**Files:**
- Create: `ResQ.Simulation.Engine/Entities/Structure.cs`

- [ ] **Step 1: Create Structure entity**

Create `ResQ.Simulation.Engine/Entities/Structure.cs`:

```csharp
// (license header)
using System.Numerics;

namespace ResQ.Simulation.Engine.Entities;

/// <summary>
/// Damage states for a structure.
/// </summary>
public enum DamageState
{
    /// <summary>No damage.</summary>
    Intact,

    /// <summary>Partial damage — still navigable around.</summary>
    Damaged,

    /// <summary>Fully collapsed — larger collision footprint.</summary>
    Collapsed,

    /// <summary>Submerged in flood water.</summary>
    Flooded,

    /// <summary>On fire.</summary>
    OnFire
}

/// <summary>
/// A building or infrastructure element in the simulation world.
/// </summary>
public sealed class Structure
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; }

    /// <summary>World position (center of base).</summary>
    public Vector3 Position { get; }

    /// <summary>Bounding box half-extents in meters.</summary>
    public Vector3 HalfExtents { get; }

    /// <summary>Current damage state.</summary>
    public DamageState DamageState { get; set; }

    /// <summary>
    /// Creates a new structure.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="position">Center of base position.</param>
    /// <param name="halfExtents">Bounding box half-extents (width/2, height/2, depth/2).</param>
    public Structure(string id, Vector3 position, Vector3 halfExtents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        Id = id;
        Position = position;
        HalfExtents = halfExtents;
        DamageState = DamageState.Intact;
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build ResQ.Simulation.Engine/ -c Release`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add ResQ.Simulation.Engine/Entities/Structure.cs
git commit -m "feat(sim-engine): add Structure entity with damage states"
```

---

### Task 11: Implement SimulationWorld

**Files:**
- Create: `ResQ.Simulation.Engine/Core/SimulationWorld.cs`
- Create: `tests/ResQ.Simulation.Engine.Tests/Core/SimulationWorldTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/ResQ.Simulation.Engine.Tests/Core/SimulationWorldTests.cs`:

```csharp
// (license header)
using System.Numerics;
using FluentAssertions;
using NSubstitute;
using ResQ.Simulation.Engine.Core;
using ResQ.Simulation.Engine.Entities;
using ResQ.Simulation.Engine.Environment;
using ResQ.Simulation.Engine.Physics;
using Xunit;

namespace ResQ.Simulation.Engine.Tests.Core;

public class SimulationWorldTests
{
    private static SimulationWorld CreateWorld(SimulationConfig? config = null)
    {
        config ??= new SimulationConfig { ClockMode = ClockMode.Stepped, Seed = 42 };
        var terrain = Substitute.For<ITerrain>();
        terrain.GetElevation(Arg.Any<double>(), Arg.Any<double>()).Returns(0.0);
        var weather = Substitute.For<IWeatherSystem>();
        weather.GetWind(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>()).Returns(Vector3.Zero);
        weather.Visibility.Returns(1.0);
        return new SimulationWorld(config, terrain, weather);
    }

    [Fact]
    public void Constructor_InitializesEmptyWorld()
    {
        var world = CreateWorld();

        world.Drones.Should().BeEmpty();
        world.Structures.Should().BeEmpty();
        world.Clock.ElapsedTime.Should().Be(0);
    }

    [Fact]
    public void AddDrone_AddsToCollection()
    {
        var world = CreateWorld();

        world.AddDrone("drone-1", new Vector3(0, 50, 0));

        world.Drones.Should().HaveCount(1);
        world.Drones[0].Id.Should().Be("drone-1");
    }

    [Fact]
    public void AddDrone_DuplicateId_Throws()
    {
        var world = CreateWorld();
        world.AddDrone("drone-1", new Vector3(0, 50, 0));

        var act = () => world.AddDrone("drone-1", new Vector3(10, 50, 0));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Step_AdvancesClockAndDrones()
    {
        var world = CreateWorld();
        world.AddDrone("drone-1", new Vector3(0, 50, 0));
        world.Drones[0].SendCommand(FlightCommand.GoTo(new Vector3(100, 50, 0)));

        world.Step();

        world.Clock.ElapsedTime.Should().BeGreaterThan(0);
        world.Drones[0].FlightModel.State.Position.X.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Step_MultipleSteps_Deterministic()
    {
        var world1 = CreateWorld(new SimulationConfig { ClockMode = ClockMode.Stepped, Seed = 42 });
        var world2 = CreateWorld(new SimulationConfig { ClockMode = ClockMode.Stepped, Seed = 42 });

        world1.AddDrone("d1", new Vector3(0, 50, 0));
        world2.AddDrone("d1", new Vector3(0, 50, 0));

        world1.Drones[0].SendCommand(FlightCommand.GoTo(new Vector3(100, 50, 0)));
        world2.Drones[0].SendCommand(FlightCommand.GoTo(new Vector3(100, 50, 0)));

        for (int i = 0; i < 60; i++)
        {
            world1.Step();
            world2.Step();
        }

        world1.Drones[0].FlightModel.State.Position.Should().Be(
            world2.Drones[0].FlightModel.State.Position);
    }

    [Fact]
    public void AddStructure_AddsToCollection()
    {
        var world = CreateWorld();
        world.AddStructure("bldg-1", new Vector3(50, 0, 50), new Vector3(5, 10, 5));

        world.Structures.Should().HaveCount(1);
        world.Structures[0].Id.Should().Be("bldg-1");
    }

    [Fact]
    public void Step_WhenPaused_DoesNotAdvance()
    {
        var world = CreateWorld();
        world.AddDrone("d1", new Vector3(0, 50, 0));
        world.Drones[0].SendCommand(FlightCommand.GoTo(new Vector3(100, 50, 0)));

        world.Clock.Pause();
        world.Step();

        world.Clock.ElapsedTime.Should().Be(0);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~SimulationWorldTests" -v minimal`
Expected: Build fails — `SimulationWorld` does not exist

- [ ] **Step 3: Implement SimulationWorld**

Create `ResQ.Simulation.Engine/Core/SimulationWorld.cs`:

```csharp
// (license header)
using System.Numerics;
using Microsoft.Extensions.Options;
using ResQ.Simulation.Engine.Entities;
using ResQ.Simulation.Engine.Environment;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Simulation.Engine.Core;

/// <summary>
/// Top-level simulation container. Holds all entities, environment systems,
/// and drives the simulation loop.
/// </summary>
public sealed class SimulationWorld
{
    private readonly SimulationConfig _config;
    private readonly ITerrain _terrain;
    private readonly IWeatherSystem _weather;
    private readonly List<SimulatedDrone> _drones = new();
    private readonly List<Structure> _structures = new();
    private readonly HashSet<string> _droneIds = new();
    private readonly HashSet<string> _structureIds = new();

    /// <summary>The simulation clock.</summary>
    public SimulationClock Clock { get; }

    /// <summary>All drones in the simulation.</summary>
    public IReadOnlyList<SimulatedDrone> Drones => _drones;

    /// <summary>All structures in the simulation.</summary>
    public IReadOnlyList<Structure> Structures => _structures;

    /// <summary>The terrain.</summary>
    public ITerrain Terrain => _terrain;

    /// <summary>The weather system.</summary>
    public IWeatherSystem Weather => _weather;

    /// <summary>Seeded random for deterministic simulation.</summary>
    public Random Random { get; }

    /// <summary>
    /// Creates a new simulation world.
    /// </summary>
    /// <param name="options">Simulation configuration wrapped in IOptions.</param>
    /// <param name="terrain">Terrain provider.</param>
    /// <param name="weather">Weather system.</param>
    public SimulationWorld(IOptions<SimulationConfig> options, ITerrain terrain, IWeatherSystem weather)
        : this(options?.Value ?? throw new ArgumentNullException(nameof(options)), terrain, weather)
    {
    }

    /// <summary>
    /// Creates a new simulation world from a raw config (for tests and direct usage).
    /// </summary>
    /// <param name="config">Simulation configuration.</param>
    /// <param name="terrain">Terrain provider.</param>
    /// <param name="weather">Weather system.</param>
    public SimulationWorld(SimulationConfig config, ITerrain terrain, IWeatherSystem weather)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        ArgumentNullException.ThrowIfNull(terrain, nameof(terrain));
        ArgumentNullException.ThrowIfNull(weather, nameof(weather));

        _config = config;
        _terrain = terrain;
        _weather = weather;
        Clock = new SimulationClock(config.ClockMode, config.DeltaTime, config.AccelerationFactor);
        Random = new Random(config.Seed);
    }

    /// <summary>
    /// Adds a drone to the simulation.
    /// </summary>
    /// <param name="id">Unique drone identifier.</param>
    /// <param name="startPosition">Initial world position.</param>
    /// <returns>The created drone.</returns>
    /// <exception cref="ArgumentException">Thrown when a drone with the same ID already exists.</exception>
    public SimulatedDrone AddDrone(string id, Vector3 startPosition)
    {
        if (!_droneIds.Add(id))
            throw new ArgumentException($"Drone with ID '{id}' already exists", nameof(id));

        var drone = new SimulatedDrone(id, startPosition, _config.FlightModel);
        _drones.Add(drone);
        return drone;
    }

    /// <summary>
    /// Adds a structure to the simulation.
    /// </summary>
    /// <param name="id">Unique structure identifier.</param>
    /// <param name="position">Center of base position.</param>
    /// <param name="halfExtents">Bounding box half-extents.</param>
    /// <returns>The created structure.</returns>
    public Structure AddStructure(string id, Vector3 position, Vector3 halfExtents)
    {
        if (!_structureIds.Add(id))
            throw new ArgumentException($"Structure with ID '{id}' already exists", nameof(id));

        var structure = new Structure(id, position, halfExtents);
        _structures.Add(structure);
        return structure;
    }

    /// <summary>
    /// Advances the simulation by one fixed timestep.
    /// Updates clock, weather, and all drone entities.
    /// Does nothing if the clock is paused (clock.Advance() is a no-op when paused).
    /// </summary>
    public void Step()
    {
        var wasPaused = Clock.IsPaused;
        Clock.Advance();

        // If paused, clock didn't advance — skip everything else
        if (wasPaused) return;

        var dt = Clock.EffectiveDeltaTime;
        _weather.Step(dt);

        foreach (var drone in _drones)
        {
            if (drone.FlightModel.HasLanded) continue;

            var pos = drone.FlightModel.State.Position;
            var wind = _weather.GetWind(pos.X, pos.Y, pos.Z);
            drone.Step(dt, wind);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ --filter "FullyQualifiedName~SimulationWorldTests" -v minimal`
Expected: All 7 tests pass

- [ ] **Step 5: Run full test suite**

Run: `dotnet test tests/ResQ.Simulation.Engine.Tests/ -v minimal`
Expected: All tests pass (clock + kinematic + quadrotor + terrain + weather + drone + world)

- [ ] **Step 6: Commit**

```bash
git add ResQ.Simulation.Engine/Core/SimulationWorld.cs tests/ResQ.Simulation.Engine.Tests/Core/SimulationWorldTests.cs
git commit -m "feat(sim-engine): implement SimulationWorld with fixed-timestep sim loop"
```

---

## Chunk 5: Full build verification

### Task 12: Verify full solution builds and tests pass

- [ ] **Step 1: Build entire solution**

Run: `dotnet build -c Release`
Expected: All projects build with no errors

- [ ] **Step 2: Run all tests**

Run: `dotnet test -c Release`
Expected: All tests pass (existing + new engine tests)

- [ ] **Step 3: Check formatting**

Run: `dotnet format --verify-no-changes`
Expected: No formatting issues

- [ ] **Step 4: Final commit if any formatting fixes needed**

If formatting issues were found in step 3:
```bash
dotnet format
git add -A
git commit -m "style(sim-engine): apply dotnet format"
```

---

## Summary

**Phase 1 delivers:**
- `ResQ.Simulation.Engine` project with core sim loop, clock, config
- `KinematicFlightModel` for scale tests (10K+ drones)
- `QuadrotorFlightModel` for fidelity testing (6DOF dynamics)
- `HeightmapTerrain` with bilinear interpolation
- `WeatherSystem` with calm/steady/turbulent wind modes
- `SimulatedDrone` entity bridging flight models and sensors
- `Structure` entity with damage states
- `SimulationWorld` orchestrating everything with deterministic stepping
- Full test coverage with deterministic stepped-clock assertions

**Phase 2 (separate plan) will add:**
- `CollisionSystem` (BepuPhysics2 integration — terrain heightfield, structure shapes, drone spheres)
- `FireSpreadSimulation` + `FloodSimulation`
- `HazardZone` entity
- Scenario loading/recording
- gRPC API (`SimulationGrpcService` + `CommandGrpcService`)
- Headless CLI runner
- BepuPhysics2 and Grpc.AspNetCore package dependencies
