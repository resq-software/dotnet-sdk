# ResQ Simulation Suite — Design Spec

## Overview

A layered simulation suite for the ResQ drone disaster-response platform. Provides physics-accurate drone simulation, dynamic disaster environments, scale/load testing, digital twin capabilities, and a Unity-based 3D visualizer with operator command-and-control and scenario authoring.

## Architecture

Hybrid custom .NET 9 engine + open-source physics libraries (BepuPhysics2). Unity as a separate visualization/authoring layer communicating via gRPC.

### Layers

| Layer | Tech | Purpose |
|-------|------|---------|
| Sim Engine | .NET 9 (`ResQ.Simulation.Engine`) | Physics, drone behavior, environment, scenarios |
| Sim API | gRPC/protobuf | Streams state from engine to any frontend |
| Visualizer | Unity (separate repo) | 3D rendering, C2, scenario editor |
| Headless Runner | CLI (`ResQ.Simulation.Headless`) | CI integration, scale testing, no GPU needed |

### Deployment Targets

- Developer workstations (visual + headless)
- CI pipelines (headless only, `dotnet run`)
- Standalone demo app (Unity build)

## Project Structure

```
dotnet-sdk/
├── ResQ.Simulation/                    # Existing — thin wrapper/legacy compat
├── ResQ.Simulation.Engine/             # Core sim engine, physics, environment
├── ResQ.Simulation.Engine.Tests/       # xUnit tests for engine
├── ResQ.Simulation.Headless/           # CLI runner for CI and scale tests
└── protos/simulation/                  # Sim-specific proto definitions

resq-simulation-unity/                  # Separate repo — Unity visualizer + scenario editor
```

### Engine Internal Structure

```
ResQ.Simulation.Engine/
├── Core/
│   ├── ISimulationClock.cs
│   ├── SimulationWorld.cs
│   └── SimulationConfig.cs
├── Physics/
│   ├── IFlightModel.cs
│   ├── KinematicFlightModel.cs
│   ├── QuadrotorFlightModel.cs
│   └── CollisionSystem.cs
├── Environment/
│   ├── ITerrain.cs
│   ├── IWeatherSystem.cs
│   ├── IHazardSimulation.cs
│   ├── FireSpreadSimulation.cs
│   └── FloodSimulation.cs
├── Entities/
│   ├── SimulatedDrone.cs
│   ├── Structure.cs
│   └── HazardZone.cs
├── Scenarios/
│   ├── IScenario.cs
│   ├── ScenarioLoader.cs
│   └── ScenarioRecorder.cs
└── Api/
    ├── SimulationGrpcService.cs
    └── CommandGrpcService.cs
```

## Simulation Loop & Time Management

Fixed-timestep loop (default 1/60s), decoupled from rendering:

```
while (running)
{
    clock.Advance(dt);
    weatherSystem.Step(dt);
    foreach (var hazard in hazards)
        hazard.Step(dt);
    foreach (var drone in drones)
    {
        drone.FlightModel.Step(dt);
        collisionSystem.Check(drone);
        drone.UpdateSensors(dt);
    }
    scenarioRecorder.Snapshot(world);
    grpcService.BroadcastState(world);
}
```

### Clock Modes

| Mode | Use Case | Behavior |
|------|----------|----------|
| `RealTime` | Demo, digital twin | 1:1 wall clock |
| `Accelerated(factor)` | Quick scenario runs | 10x, 100x speed |
| `Stepped` | Unit tests, CI | Advance manually per frame, fully deterministic |

### Determinism

Seeded `Random` per scenario (not `Random.Shared`). Same seed + same inputs = identical simulation. Enables reliable test assertions and exact replay.

## Physics Layer

### IFlightModel Interface

```csharp
public interface IFlightModel
{
    DronePhysicsState State { get; }
    void ApplyCommand(FlightCommand command);
    void Step(double dt, Vector3 windAtPosition);
}
```

### KinematicFlightModel

- Position += velocity * dt, velocity += acceleration * dt
- Max speed, turn rate, climb rate constraints
- Wind as simple velocity offset
- Cheap enough for 10K+ drones simultaneously

### QuadrotorFlightModel

- 6DOF rigid body: position, orientation (quaternion), linear + angular velocity
- Four rotors with individual thrust/torque based on RPM
- Aerodynamic drag proportional to velocity squared
- Wind as force vector sampled from IWeatherSystem at drone position
- BepuPhysics2 handles rigid body integration and collision response
- Battery drain modeled as function of total thrust

### Collision (BepuPhysics2)

- Terrain: heightfield collision shape
- Structures: compound convex shapes with damage-state-dependent geometry
- Drones: sphere colliders
- Collision events trigger drone state changes (emergency landing, damage)

Scenarios select flight model via config:

```json
{ "flightModel": "kinematic", "droneCount": 5000 }
{ "flightModel": "quadrotor", "droneCount": 12, "windProfile": "gusty" }
```

## Environment & Dynamic Hazards

### Terrain (ITerrain)

- Heightmap-based (PNG or raw float arrays)
- `GetElevation(lat, lon)` — meters above sea level
- `GetSurfaceType(lat, lon)` — vegetation, water, urban, bare ground
- Built-in maps (flat, hilly, urban grid) for testing; custom maps from scenario files

### Weather (IWeatherSystem)

- Wind field: `GetWind(lat, lon, altitude)` returns Vector3 force
- Modes: `Calm`, `Steady(direction, speed)`, `Turbulent(seed)` with Perlin noise gusts
- Visibility: 0.0-1.0 scalar, affects detection probability
- Precipitation: scales battery drain, reduces detection confidence

### Fire Spread (FireSpreadSimulation)

- Cellular automata on grid overlaying terrain
- Cell states: `Unburned | Burning | BurnedOut`
- Spread rate depends on: wind direction/speed, surface type, terrain slope
- Rothermel-inspired model, simplified for real-time

### Flood (FloodSimulation)

- Shallow water equations on terrain heightmap grid
- Water sources inject volume per timestep
- Water flows downhill, pools in low areas, rises over time
- Structures below water level transition to `Flooded` damage state

### HazardZone Entity

```csharp
public class HazardZone
{
    public HazardType Type { get; }
    public IHazardSimulation Simulation { get; }
    public BoundingBox AffectedArea { get; }    // grows over time
    public double Intensity { get; }            // 0.0-1.0
}
```

Drones in hazard zones experience: reduced visibility, GPS noise in smoke, forced altitude changes near fire thermals. QuadrotorFlightModel responds physically; KinematicFlightModel applies probability modifiers.

## gRPC API

### SimulationStream (engine to Unity) — server-streaming

```protobuf
service SimulationStream {
  rpc StreamWorldState(StreamRequest) returns (stream WorldStateFrame);
  rpc StreamReplay(ReplayRequest) returns (stream WorldStateFrame);
}

message WorldStateFrame {
  double timestamp = 1;
  repeated DroneState drones = 2;
  repeated HazardState hazards = 3;
  WeatherState weather = 4;
  repeated StructureState structures = 5;
  repeated DetectionEvent detections = 6;
}
```

### SimulationCommand (Unity to engine) — unary RPCs

```protobuf
service SimulationCommand {
  rpc SendDroneCommand(DroneCommand) returns (CommandAck);
  rpc DeploySwarm(SwarmRequest) returns (CommandAck);
  rpc PlaceHazard(PlaceHazardRequest) returns (CommandAck);
  rpc SetWeather(WeatherConfig) returns (CommandAck);
  rpc SaveScenario(SaveRequest) returns (ScenarioMetadata);
  rpc LoadScenario(LoadRequest) returns (CommandAck);
  rpc SetClockMode(ClockModeRequest) returns (CommandAck);
  rpc Pause(Empty) returns (CommandAck);
  rpc Resume(Empty) returns (CommandAck);
}
```

### Digital Twin Mode

SimulatedDrone optionally forwards telemetry to real CoordinationHceClient and InfrastructureApiClient. When enabled, the sim engine is indistinguishable from a fleet of real drones hitting production APIs.

### Bandwidth

WorldStateFrame at 30Hz with 100 drones is ~50KB/s. For 10K drones headless, stream is off (no subscriber), zero overhead.

## Headless Runner & CI

### CLI Usage

```bash
# Single scenario
dotnet run --project ResQ.Simulation.Headless -- run scenarios/wildfire-swarm.json

# Scale test
dotnet run --project ResQ.Simulation.Headless -- stress --drones 5000 --speed 100x

# CI mode: all scenarios, JUnit XML output
dotnet run --project ResQ.Simulation.Headless -- ci --scenarios scenarios/ --output results.xml
```

### Scenario JSON Format

```json
{
  "name": "Wildfire evacuation",
  "seed": 42,
  "clockMode": "accelerated",
  "clockFactor": 10,
  "terrain": "builtin:hilly",
  "weather": { "mode": "steady", "direction": 270, "speed": 15 },
  "flightModel": "kinematic",
  "drones": [
    { "id": "scout-{i}", "count": 20, "startZone": "grid:37.77,-122.41,0.01" }
  ],
  "hazards": [
    { "type": "fire", "origin": [37.775, -122.415], "startTime": 0 }
  ],
  "assertions": [
    { "type": "allDetected", "hazardType": "fire", "withinSeconds": 120 },
    { "type": "noCrashes" },
    { "type": "allLanded", "withinSeconds": 600 }
  ]
}
```

### Assertion Types

- `allDetected` — every hazard detected within time limit
- `noCrashes` — no drones hit terrain or structures
- `allLanded` — all drones safely landed before battery death
- `maxLatency` — telemetry to backend under threshold (digital twin mode)
- Custom via `IScenarioAssertion` interface

## Unity Visualizer (Separate Repo, Later Phase)

### Responsibilities

| Feature | Description |
|---------|-------------|
| World renderer | Terrain, structures as prefabs, hazard VFX |
| Drone renderer | 3D models, rotor animation, path trails, status indicators |
| HUD overlays | Battery, detection markers, swarm lines, heat maps |
| Command panel | Select drones, set waypoints, deploy swarms, trigger RTL |
| Scenario editor | Place structures, paint hazards, set weather, save/load JSON |
| Timeline scrubber | Pause/resume/seek replays via StreamReplay |
| Camera system | Free cam, follow drone, top-down tactical view |

### Integration

- Communicates exclusively via gRPC (SimulationStream + SimulationCommand)
- Generates own C# types from `protos/simulation/` via gRPC-for-Unity
- Never directly references ResQ.Simulation.Engine assemblies
- Shared protos, not shared DLLs

### Phasing

The engine, headless runner, and gRPC API deliver immediate value without Unity. Unity adds the visual layer when ready.

## Dependencies

| Package | Purpose | License |
|---------|---------|---------|
| BepuPhysics2 | Collision detection, rigid body dynamics | Apache-2.0 |
| Grpc.AspNetCore | gRPC server for sim API | Apache-2.0 |
| Google.Protobuf | Protobuf serialization | BSD-3 |
| System.Numerics.Vectors | Vector3, Quaternion math | MIT |

## Decisions

- **Unity in separate repo** — different build pipeline, avoids polluting SDK git history
- **gRPC over HTTP** — server-streaming for continuous world state, protobuf already in stack
- **BepuPhysics2 over custom physics** — MIT-licensed, pure C#, battle-tested, avoids reinventing collision detection
- **Dual flight models** — kinematic for scale, quadrotor for fidelity, same interface
- **Deterministic seeded random** — enables reliable CI assertions and exact replay
- **Scenario JSON with assertions** — CI gate without custom test code per scenario
