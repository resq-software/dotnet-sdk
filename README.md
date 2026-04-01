# ResQ .NET SDK

[![CI](https://img.shields.io/github/actions/workflow/status/resq-software/dotnet-sdk/ci.yml?branch=main&label=ci&style=flat-square)](https://github.com/resq-software/dotnet-sdk/actions)
[![ResQ.Core](https://img.shields.io/nuget/v/ResQ.Core?style=flat-square&label=ResQ.Core)](https://www.nuget.org/packages/ResQ.Core)
[![ResQ.Mavlink](https://img.shields.io/nuget/v/ResQ.Mavlink?style=flat-square&label=ResQ.Mavlink)](https://www.nuget.org/packages/ResQ.Mavlink)
[![ResQ.Protocols](https://img.shields.io/nuget/v/ResQ.Protocols?style=flat-square&label=ResQ.Protocols)](https://www.nuget.org/packages/ResQ.Protocols)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue?style=flat-square)](LICENSE)

> Full-stack .NET 9 SDK for the ResQ autonomous drone disaster-response platform: MAVLink protocol, ArduPilot SITL integration, mesh networking, protocol gateway, blockchain anchoring, and physics-based simulation.

---

## Architecture

```mermaid
graph TB
    subgraph External["External Systems"]
        AP["ArduPilot / PX4<br/>Flight Controllers"]
        GCS["QGroundControl<br/>Mission Planner"]
        NEO["Neo N3 Blockchain"]
        IPFS["IPFS / Pinata"]
        API["ResQ Infrastructure API"]
    end

    subgraph SDK["ResQ .NET SDK"]
        subgraph MAVLink["MAVLink Stack"]
            ML["ResQ.Mavlink<br/><i>Codec, 62 messages, UDP/TCP/Serial</i>"]
            MD["ResQ.Mavlink.Dialect<br/><i>8 custom messages (60000-60007)</i>"]
            MM["ResQ.Mavlink.Mesh<br/><i>Flooding, relay, radio model</i>"]
            MS["ResQ.Mavlink.Sitl<br/><i>ArduPilot SITL bridge</i>"]
            MG["ResQ.Mavlink.Gateway<br/><i>MAVLink ↔ Protobuf translation</i>"]
        end

        subgraph Simulation["Simulation"]
            SE["ResQ.Simulation.Engine<br/><i>Physics, terrain, weather</i>"]
            SH["ResQ.Simulation<br/><i>SITL harness, scenarios</i>"]
        end

        subgraph Platform["Platform Services"]
            CL["ResQ.Clients<br/><i>HTTP clients, Polly resilience</i>"]
            BC["ResQ.Blockchain<br/><i>Neo N3 anchoring</i>"]
            ST["ResQ.Storage<br/><i>IPFS / Pinata</i>"]
        end

        subgraph Core["Shared"]
            CO["ResQ.Core<br/><i>Models, enums, interfaces</i>"]
            PR["ResQ.Protocols<br/><i>gRPC / Protobuf</i>"]
        end
    end

    AP <-->|"MAVLink v2"| ML
    GCS <-->|"UDP passthrough"| MG
    NEO <-->|"RPC"| BC
    IPFS <-->|"HTTP"| ST
    API <-->|"REST / gRPC"| CL

    ML --> MD
    ML --> MM
    ML --> MS
    ML --> MG
    MS --> SE
    MG --> CO
    MG --> PR
    CL --> CO
    CL --> PR
    SE --> SH
```

## Project Map

```mermaid
graph LR
    subgraph mavlink["ResQ.Mavlink (standalone)"]
        codec["Codec<br/>CRC, parse, serialize"]
        msgs["62 Message Types<br/>readonly record structs"]
        transport["Transports<br/>UDP, TCP, Serial"]
        conn["Connection<br/>heartbeat, commands, missions"]
    end

    subgraph dialect["ResQ.Mavlink.Dialect"]
        dmsg["8 Custom Messages<br/>60000-60007"]
        dreg["Dialect Registry"]
        dtrans["Dialect Translator"]
    end

    subgraph mesh["ResQ.Mavlink.Mesh"]
        mt["Mesh Transport<br/>TTL flooding, dedup, priority queue"]
        nb["Neighbor Table<br/>topology, partition detection"]
        rl["Mesh Relay<br/>store-and-forward"]
        rm["Radio Model<br/>range, LOS, attenuation"]
        fw["Firmware Hooks<br/>IFirmwareIntegration API"]
    end

    subgraph gateway["ResQ.Mavlink.Gateway"]
        gw["Gateway Orchestrator<br/>IHostedService"]
        tr["Message Translator<br/>MAVLink ↔ Protobuf"]
        rt["Gateway Router<br/>rate limiting, filtering"]
        vs["Vehicle State Tracker"]
        gcs["GCS Passthrough<br/>QGC / Mission Planner"]
    end

    subgraph sitl["ResQ.Mavlink.Sitl"]
        fb["IFlightBackend<br/>async abstraction"]
        ap["ArduPilot Backend<br/>SITL process management"]
        jp["JSON Physics Bridge<br/>400 Hz sensor feed"]
        tm["Telemetry Mapper"]
    end

    codec --> msgs --> transport --> conn
    dmsg --> dreg
    mt --> nb --> rl
    gw --> tr --> rt --> vs
```

## Packages

| Package | Description | Install |
|---------|-------------|---------|
| **ResQ.Mavlink** | MAVLink v2 codec, 62 messages, UDP/TCP/Serial transports | `dotnet add package ResQ.Mavlink` |
| **ResQ.Mavlink.Dialect** | Custom ResQ MAVLink messages (detection, swarm, hazard, mesh, beacon) | `dotnet add package ResQ.Mavlink.Dialect` |
| **ResQ.Mavlink.Mesh** | Mesh transport with flooding, relay, radio simulation | `dotnet add package ResQ.Mavlink.Mesh` |
| **ResQ.Mavlink.Sitl** | ArduPilot SITL bridge with `IFlightBackend` abstraction | `dotnet add package ResQ.Mavlink.Sitl` |
| **ResQ.Mavlink.Gateway** | Protocol gateway (MAVLink ↔ Protobuf) as `IHostedService` | `dotnet add package ResQ.Mavlink.Gateway` |
| **ResQ.Simulation.Engine** | Physics-based drone simulation (kinematic + quadrotor models) | `dotnet add package ResQ.Simulation.Engine` |
| **ResQ.Core** | Domain models, enums, service interfaces | `dotnet add package ResQ.Core` |
| **ResQ.Protocols** | gRPC/Protobuf contract definitions | `dotnet add package ResQ.Protocols` |
| **ResQ.Clients** | Typed HTTP clients with Polly resilience | `dotnet add package ResQ.Clients` |
| **ResQ.Blockchain** | Neo N3 blockchain anchoring | `dotnet add package ResQ.Blockchain` |
| **ResQ.Storage** | IPFS/Pinata evidence storage | `dotnet add package ResQ.Storage` |

## Quick Start

### Send MAVLink commands to a drone

```csharp
using ResQ.Mavlink.Transport;
using ResQ.Mavlink.Connection;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Enums;

// Connect via UDP
await using var transport = new UdpTransport(new UdpTransportOptions
{
    ListenPort = 14550,
    RemotePort = 5760,
});
await using var connection = new MavlinkConnection(transport, new MavlinkConnectionOptions());

// Arm the drone
await connection.SendMessageAsync(new CommandLong
{
    TargetSystem = 1,
    TargetComponent = 1,
    Command = MavCmd.ComponentArmDisarm,
    Param1 = 1.0f,
});

// Receive telemetry
await foreach (var packet in transport.ReceiveAsync())
{
    if (MessageRegistry.TryDeserialize(packet.MessageId, packet.Payload.ToArray(), out var msg)
        && msg is GlobalPositionInt pos)
    {
        Console.WriteLine($"Lat: {pos.Lat / 1e7:F6}, Lon: {pos.Lon / 1e7:F6}, Alt: {pos.Alt / 1000.0:F1}m");
    }
}
```

### Run the protocol gateway

```csharp
using ResQ.Mavlink.Gateway;
using Microsoft.Extensions.Options;

await using var gateway = new MavlinkGateway(
    Options.Create(new MavlinkGatewayOptions { VehicleListenPort = 14550 }),
    Options.Create(new GatewayRoutingOptions()),
    Options.Create(new GcsPassthroughOptions { Enabled = true })
);

await gateway.StartAsync(CancellationToken.None);

// Read translated ResQ telemetry
await foreach (var telemetry in gateway.TelemetryFeed(CancellationToken.None))
{
    Console.WriteLine($"[{telemetry.DroneId}] {telemetry.Position.Latitude:F6}, " +
        $"{telemetry.Position.Longitude:F6} | Battery: {telemetry.BatteryPercent}%");
}
```

### Simulate a drone swarm

```csharp
using ResQ.Simulation.Engine.Core;
using ResQ.Simulation.Engine.Environment;
using ResQ.Simulation.Engine.Physics;
using Microsoft.Extensions.Options;
using System.Numerics;

var config = new SimulationConfig { FlightModel = FlightModelType.Quadrotor };
var world = new SimulationWorld(
    Options.Create(config),
    new FlatTerrain(),
    new WeatherSystem(Options.Create(new WeatherConfig()))
);

// Spawn 10 drones in formation
for (var i = 0; i < 10; i++)
    world.AddDrone($"drone-{i}", new Vector3(i * 50, 0, 0));

// Run 60 seconds of simulation at 60 Hz
for (var tick = 0; tick < 3600; tick++)
{
    world.Step();
    if (tick % 60 == 0) // Log every second
    {
        var drone = world.Drones[0];
        var state = drone.FlightModel.State;
        Console.WriteLine($"t={world.Clock.ElapsedTime:F1}s " +
            $"pos=({state.Position.X:F1}, {state.Position.Y:F1}, {state.Position.Z:F1}) " +
            $"battery={state.BatteryPercent:F0}%");
    }
}
```

### Test mesh networking

```csharp
using ResQ.Mavlink.Mesh;
using ResQ.Mavlink.Mesh.Simulation;
using Microsoft.Extensions.Options;
using System.Numerics;

var radio = new RadioModel(Options.Create(new RadioModelOptions
{
    MaxRangeMetres = 500,
}));

// Three drones: A can reach B, B can reach C, but A cannot reach C directly
var droneA = new Vector3(0, 50, 0);
var droneB = new Vector3(300, 50, 0);
var droneC = new Vector3(700, 50, 0);

Console.WriteLine($"A-B: {radio.CanCommunicate(droneA, droneB)}");  // true  (300m < 500m)
Console.WriteLine($"A-C: {radio.CanCommunicate(droneA, droneC)}");  // false (700m > 500m)
Console.WriteLine($"B-C: {radio.CanCommunicate(droneB, droneC)}");  // true  (400m < 500m)
// B relays messages between A and C via mesh transport
```

## Custom ResQ MAVLink Dialect

The SDK extends MAVLink with 8 disaster-response messages in the 60000-60255 reserved range:

```mermaid
graph LR
    subgraph OnMAVLink["Over MAVLink (works offline/mesh)"]
        D["RESQ_DETECTION<br/>60000"]
        DA["RESQ_DETECTION_ACK<br/>60001"]
        ST["RESQ_SWARM_TASK<br/>60002"]
        SA["RESQ_SWARM_TASK_ACK<br/>60003"]
        HZ["RESQ_HAZARD_ZONE<br/>60004"]
        MT["RESQ_MESH_TOPOLOGY<br/>60005"]
        DC["RESQ_DRONE_CAPABILITY<br/>60006"]
        EB["RESQ_EMERGENCY_BEACON<br/>60007"]
    end

    subgraph OnGRPC["Over gRPC (needs cloud)"]
        EA["Evidence Anchoring"]
        MA["Mission Audit"]
        IR["Incident Reports"]
        TH["Telemetry History"]
    end

    style D fill:#e74c3c,color:#fff
    style EB fill:#e74c3c,color:#fff
    style HZ fill:#f39c12,color:#fff
    style ST fill:#3498db,color:#fff
```

Register the dialect at startup:

```csharp
using ResQ.Mavlink.Dialect;

// One-time registration — enables dialect messages in codec and registry
ResqDialectRegistry.Register();
```

## Protocol Gateway Data Flow

```mermaid
sequenceDiagram
    participant Drone as ArduPilot Drone
    participant GW as MavlinkGateway
    participant GCS as QGroundControl
    participant SVC as ResQ Services

    Drone->>GW: MAVLink Heartbeat + Position
    GW->>GW: VehicleStateTracker.Update()
    GW->>GW: GatewayRouter.ShouldForward?

    alt Telemetry (rate limited to 10 Hz)
        GW->>SVC: TelemetryPacket (protobuf)
    end

    GW->>GCS: Forward raw MAVLink (passthrough)

    SVC->>GW: CommandLong (arm drone)
    GW->>Drone: MAVLink CommandLong
    GW->>GCS: Suppress GCS commands (2s priority window)
```

## Mesh Transport

```mermaid
graph TB
    subgraph Swarm["Drone Swarm (infrastructure down)"]
        A["Drone A<br/>Detects fire"]
        B["Drone B<br/>Relay"]
        C["Drone C<br/>Has ground link"]
        GND["Ground Station"]
    end

    A -->|"RESQ_DETECTION<br/>TTL=3"| B
    B -->|"TTL=2"| C
    C -->|"TTL=1"| GND

    A -.->|"Out of range"| C
    A -.->|"Out of range"| GND

    subgraph MeshBehavior["Mesh Transport Behavior"]
        direction LR
        PQ["Priority Queue<br/>Emergency > Detection > Telemetry"]
        DD["Dedup Ring Buffer<br/>sysId + seqNum hash"]
        SF["Store & Forward<br/>Buffer when partitioned"]
    end
```

## ArduPilot SITL Integration

```mermaid
sequenceDiagram
    participant Engine as SimulationWorld
    participant Backend as ArduPilotSitlBackend
    participant SITL as ArduPilot SITL Process
    participant MAV as MavlinkConnection

    Engine->>Backend: StepAsync(dt, wind)
    Backend->>SITL: JSON physics (400 Hz)<br/>accel, gyro, GPS, baro
    SITL->>MAV: MAVLink telemetry (UDP)
    MAV->>Backend: GlobalPositionInt + Attitude
    Backend->>Backend: SitlTelemetryMapper
    Backend->>Engine: DronePhysicsState

    Note over Engine,SITL: Your engine owns the world.<br/>ArduPilot owns the flight dynamics.
```

### Running with ArduPilot SITL

```bash
# Install ArduPilot SITL
git clone https://github.com/ArduPilot/ardupilot.git
cd ardupilot && git submodule update --init --recursive
./waf configure --board sitl && ./waf copter

# Add to PATH
export PATH="$PWD/build/sitl/bin:$PATH"

# Run SITL integration tests
cd /path/to/dotnet-sdk
dotnet test --filter "Category=Integration"
```

## Configuration

All configuration uses the `IOptions<T>` pattern:

| Class | Key Settings | Defaults |
|-------|-------------|----------|
| `MavlinkConnectionOptions` | `SystemId`, `HeartbeatInterval`, `CommandAckTimeout` | 255, 1s, 1500ms |
| `UdpTransportOptions` | `ListenPort`, `RemoteHost`, `RemotePort` | 14550, 127.0.0.1, 14550 |
| `TcpTransportOptions` | `Host`, `Port`, `ReconnectDelay`, `IsServer` | 127.0.0.1, 5760, 2s, false |
| `MavlinkGatewayOptions` | `VehicleListenPort`, `GatewaySystemId` | 14550, 255 |
| `GatewayRoutingOptions` | `TelemetryRateLimitHz`, `InternalOnlyMessageIds` | 10, {0} |
| `GcsPassthroughOptions` | `GcsListenPort`, `ResqPriorityOverride` | 14551, true |
| `MeshTransportOptions` | `DefaultTtl`, `EmergencyTtl`, `DeduplicationWindowSize` | 3, 7, 256 |
| `RadioModelOptions` | `MaxRangeMetres`, `AttenuationFactor` | 500, 2.0 |
| `SimulationConfig` | `DeltaTime`, `Seed`, `FlightModel`, `ClockMode` | 1/60, 42, Kinematic, Stepped |
| `SitlProcessManagerOptions` | `SitlBinaryPath`, `BasePort`, `MaxInstances` | "arducopter", 5760, 20 |

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `RESQ_API_URL` | ResQ Infrastructure API endpoint | `https://api.resq.software` |
| `NEO_RPC_URL` | Neo N3 RPC endpoint | `http://localhost:10332` |
| `NEO_MOCK_MODE` | Use mock blockchain for local dev | `false` |

## Development

### Prerequisites

- .NET 9.0 SDK (or 10.x with `latestMajor` rollForward)
- Docker (optional, for packaging)
- Nix (optional, for environment parity)
- ArduPilot SITL (optional, for integration tests)

### Build and Test

```bash
git clone https://github.com/resq-software/dotnet-sdk.git
cd dotnet-sdk

dotnet build -c Release              # Build all 13 projects
dotnet test -c Release               # Run full test suite (448 tests)
dotnet format --verify-no-changes    # Check formatting (CI gate)
dotnet pack -c Release --no-build    # Produce NuGet packages
```

### Test Categories

```bash
# All unit tests (no external dependencies)
dotnet test --filter "Category!=Integration&Category!=Hardware"

# ArduPilot SITL tests (requires arducopter on PATH)
dotnet test --filter "Category=Integration"

# Serial port tests (requires real hardware)
dotnet test --filter "Category=Hardware"
```

### Shared Protobuf Source

```bash
# Sync protos from buf.build (requires BUF_TOKEN)
bash scripts/sync-protos.sh
dotnet build ResQ.Protocols/ResQ.Protocols.csproj
```

## Dependency Graph

```mermaid
graph BT
    Core["ResQ.Core"]
    Proto["ResQ.Protocols"]
    Clients["ResQ.Clients"]
    BC["ResQ.Blockchain"]
    Storage["ResQ.Storage"]
    Sim["ResQ.Simulation"]
    Engine["ResQ.Simulation.Engine"]
    ML["ResQ.Mavlink<br/><b>standalone</b>"]
    Dialect["ResQ.Mavlink.Dialect"]
    Mesh["ResQ.Mavlink.Mesh"]
    SITL["ResQ.Mavlink.Sitl"]
    GW["ResQ.Mavlink.Gateway"]

    Clients --> Core
    Clients --> Proto
    BC --> Core
    Storage --> Core
    Sim --> Core
    Sim --> Clients
    Engine -.->|"no deps"| Engine

    Dialect --> ML
    Mesh --> ML
    Mesh --> Dialect
    SITL --> ML
    SITL --> Engine
    GW --> ML
    GW --> Core
    GW --> Proto

    style ML fill:#2ecc71,color:#fff
    style Core fill:#3498db,color:#fff
    style Engine fill:#9b59b6,color:#fff
```

> `ResQ.Mavlink` has zero SDK dependencies and can be used standalone by any .NET project that needs a MAVLink v2 library.

## Contributing

We follow [Conventional Commits](https://www.conventionalcommits.org/) and [SemVer](https://semver.org/).

1. **Fork** the repository
2. **Branch**: `feat/my-feature` or `fix/my-bug`
3. **Test**: all 448 tests must pass, `dotnet format` must be clean
4. **PR**: open against `main`

## License

Copyright 2026 ResQ. Licensed under the [Apache License, Version 2.0](./LICENSE).
