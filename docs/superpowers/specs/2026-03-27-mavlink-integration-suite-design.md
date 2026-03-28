# ResQ MAVLink Integration Suite — Design Spec

**Date:** 2026-03-27
**Status:** Approved
**Scope:** Full-stack MAVLink integration — client library, SITL bridge, protocol gateway, custom dialect, mesh transport

## Overview

Transform ResQ from a proprietary simulation platform into a full-stack drone system that speaks the industry-standard MAVLink protocol. Five new projects across five phases, each delivering standalone capability. Enables interop with ArduPilot, PX4, QGroundControl, Mission Planner, and the broader UAV ecosystem.

### Key Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Scope | Full stack (client, SITL, gateway, dialect, mesh, firmware) | Platform credibility in drone/disaster-response space |
| ArduPilot relationship | Custom firmware fork, full hardware deployment | ResQ as vertical stack from firmware to cloud |
| MAVLink dialect | `common` + custom ResQ dialect | Interop with standard GCS + domain-specific extensions |
| Transports | UDP + TCP + Serial + Mesh | All deployment scenarios from SITL to field ops |
| SITL integration | Layered — ResQ engine orchestrates, ArduPilot is pluggable flight backend | Preserves simulation engine investment, gets battle-tested flight dynamics |
| Protocol split | Time-critical on MAVLink, cloud ops on gRPC | MAVLink works offline/mesh; gRPC needs cloud services anyway |
| Approach | Middle-out — clean library + SITL as first consumer | Architecture quality + fast time-to-demo |

## New Projects & Solution Structure

Five new projects added to `ResQ.Sdk.sln`:

```
ResQ.Mavlink/                    # MAVLink v2 codec, messages, transports
ResQ.Mavlink.Tests/
ResQ.Mavlink.Sitl/               # ArduPilot SITL bridge (orchestrator integration)
ResQ.Mavlink.Sitl.Tests/
ResQ.Mavlink.Gateway/            # Bidirectional protobuf ↔ MAVLink translation
ResQ.Mavlink.Gateway.Tests/
ResQ.Mavlink.Dialect/            # ResQ custom MAVLink dialect definitions + codegen
ResQ.Mavlink.Dialect.Tests/
ResQ.Mavlink.Mesh/               # Mesh transport layer
ResQ.Mavlink.Mesh.Tests/
```

**Dependency graph:**

- `ResQ.Mavlink` — zero-dependency on rest of SDK, usable standalone. Targets .NET 9 + `netstandard2.1`. The `netstandard2.1` target requires `System.Memory` (for `Span<byte>`) and `Microsoft.Bcl.AsyncInterfaces` (for `IAsyncEnumerable`) as polyfill packages.
- `ResQ.Mavlink.Sitl` → `ResQ.Mavlink` + `ResQ.Simulation.Engine` (**Note:** `ResQ.Simulation.Engine` is currently in a worktree (`sim-engine`) and must be merged to `main` before Phase 1 SITL work begins. The MAVLink core library has no such dependency and can proceed independently.)
- `ResQ.Mavlink.Gateway` → `ResQ.Mavlink` + `ResQ.Protocols`
- `ResQ.Mavlink.Dialect` → `ResQ.Mavlink`
- `ResQ.Mavlink.Mesh` → `ResQ.Mavlink`

## ResQ.Mavlink Core — Codec & Transport Architecture

Three layers:

### Codec Layer

Stateless MAVLink v2 serialization/deserialization.

- `MavlinkCodec` — Parses raw bytes into `MavlinkPacket`. Emits bytes from packets. Handles v2 framing including incompatibility/compatibility flags.
- `MavlinkPacket` — Immutable record: `SystemId`, `ComponentId`, `MessageId`, `Payload`, `SequenceNumber`, `Signature?`. Uses `Span<byte>` / `Memory<byte>` for zero-allocation parsing on the hot path.
- `ICrcExtra` — Per-message CRC seed lookup. Generated from XML definitions.
- Signing support — Optional MAVLink v2 message signing (SHA-256, truncated to 48 bits per MAVLink v2 spec) for authenticated links.

### Message Layer

Strongly-typed C# representations of MAVLink messages.

- Each message is a `readonly record struct` (e.g., `Heartbeat`, `GlobalPositionInt`, `Attitude`, `CommandLong`).
- Source-generated from MAVLink XML definitions (`common.xml` + `resq.xml`).
- `IMavlinkMessage` interface with `Serialize(Span<byte>)` and static `Deserialize(ReadOnlySpan<byte>)`.
- Phase 1 implements ~25 messages (SITL minimum set):
  - Heartbeat, SysStatus, GpsRawInt, GlobalPositionInt, Attitude, VfrHud
  - CommandLong, CommandAck, MissionItemInt, MissionRequest, MissionAck, MissionCount, MissionCurrent
  - SetMode, ParamRequestRead, ParamValue, ParamSet
  - StatusText, RcChannelsOverride
  - SetPositionTargetGlobalInt, PositionTargetGlobalInt
  - HomePosition, ExtendedSysState

### Transport Layer

Pluggable async transports behind `IMavlinkTransport`:

```csharp
public interface IMavlinkTransport : IAsyncDisposable
{
    ValueTask SendAsync(MavlinkPacket packet, CancellationToken ct = default);
    IAsyncEnumerable<MavlinkPacket> ReceiveAsync(CancellationToken ct = default);
    TransportState State { get; }
    IAsyncEnumerable<TransportState> StateChanges(CancellationToken ct = default);
}
```

**Error contract:**
- `SendAsync` throws `TransportDisconnectedException` if `State` is not `Connected`. Callers are responsible for checking state or catching.
- `ReceiveAsync` yields packets while connected. On transport failure, yields a terminal error via `IAsyncEnumerable` completion (does not throw mid-stream). Consumers check `State` after enumeration ends.
- `TcpTransport` handles reconnection internally (configurable via `TcpTransportOptions.ReconnectPolicy`). During reconnect, `State` is `Reconnecting` and `SendAsync` buffers up to `MaxReconnectBuffer` packets.
- `UdpTransport` is connectionless — `State` is always `Connected` unless explicitly disposed.

- Phase 1: `UdpTransport` (SITL native, broadcast discovery)
- Phase 3: `TcpTransport`, `SerialTransport`
- Phase 5: `MeshTransport` (in `ResQ.Mavlink.Mesh`)

### Connection Management

`MavlinkConnection` sits on top of transport + codec:

- Heartbeat send/receive (1 Hz default)
- Remote system/component ID tracking
- `IAsyncEnumerable<T>` for typed message streams (filter by message type via `OfType<T>()` extension)
- Command protocol with retry + ack tracking
- Parameter protocol (`GetParamAsync`, `SetParamAsync`)
- Mission protocol (upload/download with retry)
- DI-friendly via `IOptions<MavlinkConnectionOptions>`

## SITL Bridge — Layered Integration

ArduPilot SITL instances act as pluggable flight backends for `SimulatedDrone`.

### IFlightBackend Abstraction

The simulation engine currently uses a synchronous `IFlightModel` interface with a tick-driven `Step(double dt, Vector3 wind)` method, implemented by `KinematicFlightModel` and `QuadrotorFlightModel`. The SITL bridge needs an async interface because ArduPilot communication is inherently asynchronous (UDP packets, process management).

**Migration strategy:** Introduce `IFlightBackend` as a new async abstraction that coexists with `IFlightModel`. An adapter (`FlightModelBackendAdapter`) wraps any existing `IFlightModel` into an `IFlightBackend`, so `SimulatedDrone` can be refactored to accept `IFlightBackend` without breaking existing flight models. The `SimulationWorld` tick loop calls `IFlightBackend.StepAsync()` which the adapter implements by delegating to the synchronous `IFlightModel.Step()`.

```csharp
public interface IFlightBackend : IAsyncDisposable
{
    ValueTask InitializeAsync(DroneConfig config, CancellationToken ct = default);
    ValueTask<DronePhysicsState> StepAsync(double dt, EnvironmentState env, CancellationToken ct = default);
    ValueTask SendCommandAsync(FlightCommand command, CancellationToken ct = default);
    FlightBackendCapabilities Capabilities { get; }
}
```

Note: Changed from streaming `IAsyncEnumerable<DronePhysicsState>` to tick-driven `StepAsync` to match the simulation engine's existing tick loop. The `ArduPilotSitlBackend` internally manages the async MAVLink connection but presents results synchronously per tick (latest telemetry snapshot).

Implementations:
- `FlightModelBackendAdapter` — wraps any `IFlightModel` (preserves all existing models)
- `ArduPilotSitlBackend` — new, real autopilot logic

**Files changed in Phase 1:**
- `SimulatedDrone.cs` — accept `IFlightBackend` instead of `FlightModelType` enum
- `SimulationWorld.cs` — tick loop calls `StepAsync` instead of `Step`
- `DroneConfig` — new `BackendType` property (Kinematic, Quadrotor, ArduPilotSitl)

### ArduPilotSitlBackend

Manages one ArduPilot SITL process per drone:

- **Lifecycle:** Spawns `arducopter`/`arduplane` SITL with `--model json` flag (JSON physics input mode).
- **Inbound (ArduPilot → Engine):** MAVLink telemetry over UDP → parsed by `MavlinkConnection` → mapped to `DronePhysicsState`.
- **Outbound (Engine → ArduPilot):** JSON physics socket feeds sensor data (accelerometer, gyro, GPS, barometer) at 400 Hz. Engine computes from world state (terrain, weather, hazards).
- **Commands:** `FlightCommand` translated to MAVLink `CommandLong` or `SetPositionTargetGlobalInt`.
- **Fault injection:** Existing `SimInfection` maps directly:
  - `GPS_DENIED` → corrupt/stop GPS sensor feed
  - `COMMS_LATENCY` → delay MAVLink packets
  - `SENSOR_NOISE` → inject noise into accelerometer/gyro feeds
  - `DRONE_FAILURE` → kill SITL process or force `EMERGENCY` mode

### SitlProcessManager

Fleet management for SITL processes:

- Pool-based pre-spawning for faster scenario startup
- Auto port allocation (base port + offset per instance)
- Configurable max concurrent instances (~50MB RAM each)
- Graceful cleanup on dispose, crash recovery

### Scale Story

SITL backends for high-fidelity testing (1-50 drones). Kinematic backends for swarm-scale (thousands). Mix per drone in the same scenario via `ScenarioRunner`.

## Protocol Gateway — MAVLink ↔ Protobuf Translation

Bidirectional, stateful translation between MAVLink and ResQ protobuf worlds.

### Protocol Split

| Channel | Messages | Rationale |
|---|---|---|
| MAVLink | Heartbeat, position, attitude, commands, detections, swarm coordination | Works air-to-air, mesh, degraded comms |
| gRPC | Evidence anchoring, blockchain audit, IPFS upload, mission planning, historical queries | Needs cloud services |
| Both (gateway syncs) | Telemetry, mission state, drone status | MAVLink is source of truth, gateway replicates to gRPC |

### IMavlinkGateway

```csharp
public interface IMavlinkGateway : IHostedService
{
    IReadOnlyDictionary<byte, MavlinkConnection> ConnectedSystems { get; }
    ValueTask SendToVehicleAsync(byte systemId, IMavlinkMessage msg, CancellationToken ct = default);
    ValueTask BroadcastAsync(IMavlinkMessage msg, CancellationToken ct = default);
    IObservable<TelemetryPacket> TelemetryFeed { get; }
    IObservable<MavlinkPacket> RawFeed { get; }
}
```

Runs as `IHostedService` — drop into any ASP.NET Core host or simulation runner.

### Components

- **`MessageTranslator`** — Stateless mappers. `GlobalPositionInt` ↔ `TelemetryPacket.Position` (handles degE7 scaling), `Heartbeat` → `DroneStatus` enum, etc. Pure functions, unit-testable.
- **`GatewayRouter`** — Routing rules, rate limiting, message filtering. Configurable via `GatewayRoutingOptions`.
- **`GcsPassthrough`** — Standard GCS (QGroundControl, Mission Planner) connects on separate UDP port. Bidirectional forwarding with ResQ priority override (configurable).

### State Tracking

Per-vehicle state: position, attitude, battery, mode, mission progress, link quality. Maps to existing `Telemetry` model in `ResQ.Core`.

## Custom ResQ MAVLink Dialect

Extends `common.xml` with ResQ-specific messages in the 60000-60049 range.

### Messages on MAVLink (time-critical, works offline/mesh)

| ID | Message | Purpose |
|---|---|---|
| 60000 | `RESQ_DETECTION` | Incident detection: type (FIRE/FLOOD/PERSON/VEHICLE), confidence, bounding box, GPS, timestamp |
| 60001 | `RESQ_DETECTION_ACK` | Swarm dedup — "I saw that too" or "acknowledged, investigating" |
| 60002 | `RESQ_SWARM_TASK` | Task assignment: drone ID, area polygon, search pattern, priority |
| 60003 | `RESQ_SWARM_TASK_ACK` | Accept/reject/complete task |
| 60004 | `RESQ_HAZARD_ZONE` | Hazard boundary: polygon + type + severity + progression vector |
| 60005 | `RESQ_MESH_TOPOLOGY` | Mesh state: neighbor list, link quality, relay capability |
| 60006 | `RESQ_DRONE_CAPABILITY` | Sensor/payload advertisement (thermal, RGB, LiDAR, speaker, drop mechanism) |
| 60007 | `RESQ_EMERGENCY_BEACON` | Person/vehicle in distress — high-priority mesh relay |

**Reserved range:** 60000-60255 (256 IDs) for future extensibility. Only 60000-60007 defined initially.

### What Stays on gRPC

- `EvidenceAnchor` — IPFS CID + Neo N3 tx hash
- `MissionAuditEntry` — Full audit trail with signatures
- `IncidentReport` — Aggregated multi-drone incident with evidence bundle
- `TelemetryHistory` — Historical queries and bulk replay

### Dialect Implementation

- `dialects/resq.xml` — MAVLink XML schema definition
- Source-generated C# message types (same generator as `common.xml`)
- `ResqDialectRegistry` — Registers CRC extras with codec
- Dialect version negotiated via field in `RESQ_DRONE_CAPABILITY`. On version mismatch: drones fall back to `common` messages only (guaranteed baseline). ResQ-specific messages are silently dropped if the receiver doesn't recognize the dialect version. This is safe because the protocol split ensures cloud-dependent operations use gRPC anyway.

### Gateway Integration

`MessageTranslator` maps:
- `RESQ_DETECTION` ↔ `DetectionEvent` (from `simulation.proto`)
- `RESQ_HAZARD_ZONE` ↔ `EnvironmentUpdate` (from `simulation.proto`)
- `RESQ_SWARM_TASK` ↔ new `SwarmTask` proto message (added to `core.proto`)

## Mesh Transport

Drone-to-drone communication for infrastructure-down scenarios. Lives in `ResQ.Mavlink.Mesh`, implements `IMavlinkTransport`.

### Strategy

Flooding-based with TTL. Disaster response swarms are too dynamic for complex routing tables.

### Components

- **`MeshTransport : IMavlinkTransport`** — Wraps underlying radio transport, adds mesh behavior:
  - Rebroadcast with TTL decrement on all interfaces except source
  - Dedup via ring buffer of recent packet hashes (system ID + sequence number)
  - Default TTL 3 hops; `RESQ_EMERGENCY_BEACON` gets TTL 7
  - Priority queuing: emergency > detections > normal telemetry

- **`MeshNeighborTable`** — Direct neighbor tracking:
  - Updated from `Heartbeat` packets (signal strength, last seen)
  - Publishes `RESQ_MESH_TOPOLOGY` every 5 seconds
  - Partition detection: neighbor silent for 10s → marked lost, notifies `SimulationWorld`

- **`MeshRelay`** — Store-and-forward for partitioned networks:
  - Buffers high-priority messages when no ground link
  - Flushes on connectivity restore (new neighbor with ground link)
  - Bounded buffer (default 1000 messages), priority-based eviction

### SITL Mesh Simulation

- `RadioModel` — Configurable range (default 500m), signal attenuation, LOS check against terrain heightmap
- Two drones communicate only if within range AND with LOS
- `SimInfection` can degrade radio (reduced range, increased packet loss)

### Real Hardware Path (Phase 5+)

- `SerialMeshTransport` wraps serial port to radio module (SiK, RFD900, LoRa)
- Same `IMavlinkTransport` interface
- Radio config via `MeshRadioOptions`

## Phasing

```
Phase 1 ──→ Phase 2 ──→ Phase 3 ──→ Phase 4 ──→ Phase 5
MAVLink +    Gateway     TCP/Serial   ResQ         Mesh +
SITL Bridge              + Messages   Dialect      Firmware
```

Phases 3 and 4 can run in parallel after Phase 1.

### Phase 1: ResQ.Mavlink Core + SITL Bridge

- MAVLink v2 codec (parse, serialize, CRC, signing)
- Source generator from `common.xml` → ~25 C# message types
- `UdpTransport` + `MavlinkConnection`
- `IFlightBackend` abstraction in simulation engine
- `ArduPilotSitlBackend` + `SitlProcessManager`
- Wrap existing flight models as backends
- **Deliverable:** ArduPilot SITL drone inside simulation engine, controlled by `ScenarioRunner`

### Phase 2: Protocol Gateway

- `IMavlinkGateway` as `IHostedService`
- `MessageTranslator` — bidirectional mappers
- `GatewayRouter` with configurable routing
- `GcsPassthrough` for QGroundControl/Mission Planner
- Per-vehicle state tracking
- **Deliverable:** ResQ services see drone telemetry via gRPC, GCS connects simultaneously

### Phase 3: Extended Transports + Messages

- `TcpTransport`, `SerialTransport`
- Expand to ~80 messages (full mission, sensors, RTK, gimbal, camera)
- Mission upload/download with retry and resumption
- **Deliverable:** Talk to real ArduPilot hardware over serial, full mission management

### Phase 4: ResQ Dialect

- `resq.xml` dialect definition (messages 60000-60007)
- Source-generated ResQ message types
- `ResqDialectRegistry`
- Gateway translation for ResQ messages
- New `SwarmTask` proto in `core.proto`
- **Deliverable:** Drones exchange detections, hazard zones, task assignments natively over MAVLink

### Phase 5: Mesh Transport + Firmware Hooks

- `MeshTransport` with flooding, dedup, TTL, priority queuing
- `MeshNeighborTable` + topology publishing
- `MeshRelay` store-and-forward
- `RadioModel` for SITL simulation
- `SerialMeshTransport` for real hardware
- Firmware integration hooks for ArduPilot fork
- **Deliverable:** Swarm operates in comms-denied environments, relays detections across mesh

## Configuration Defaults

| Parameter | Default | Configurable Via |
|---|---|---|
| Heartbeat interval | 1 Hz | `MavlinkConnectionOptions` |
| Command retry count | 3 | `MavlinkConnectionOptions` |
| Command ack timeout | 1500 ms | `MavlinkConnectionOptions` |
| UDP listen port | 14550 | `UdpTransportOptions` |
| TCP reconnect delay | 2s exponential backoff | `TcpTransportOptions` |
| TCP max reconnect buffer | 100 packets | `TcpTransportOptions` |
| SITL physics feed rate | 400 Hz | `SitlBackendOptions` |
| SITL max concurrent instances | 20 | `SitlProcessManagerOptions` |
| SITL RAM per instance | ~50 MB | (informational) |
| SITL base port | 5760 | `SitlProcessManagerOptions` |
| Mesh default TTL | 3 hops | `MeshTransportOptions` |
| Mesh emergency TTL | 7 hops | `MeshTransportOptions` |
| Mesh topology broadcast interval | 5 s | `MeshNeighborTableOptions` |
| Mesh neighbor timeout | 10 s | `MeshNeighborTableOptions` |
| Mesh relay buffer size | 1000 messages | `MeshRelayOptions` |
| Radio model default range | 500 m | `RadioModelOptions` |
| Gateway rate limit (telemetry) | 10 Hz per vehicle | `GatewayRoutingOptions` |

All options follow `IOptions<T>` pattern per SDK conventions.

## Testing Strategy

All tests follow existing conventions: xUnit, NSubstitute, FluentAssertions.

### Codec Tests (ResQ.Mavlink.Tests)

- Round-trip serialize/deserialize for every message type
- Fuzz: random bytes into parser — must never throw
- CRC validation against Wireshark captures from ArduPilot SITL
- Signing HMAC verification against reference implementation
- Source generator output verification

### Transport Tests

- Shared `IMavlinkTransport` contract test suite, parameterized by transport type
- `UdpTransport`: loopback, packet ordering, concurrent senders
- `TcpTransport`: connection lifecycle, reconnect, backpressure
- `SerialTransport`: mock serial port, partial read framing

### SITL Integration Tests (ResQ.Mavlink.Sitl.Tests)

- Guarded by `[Trait("Category", "Integration")]` — skipped unless SITL available
- Spawn SITL → connect → verify heartbeat
- Arm + takeoff → verify altitude change
- Waypoint mission → verify navigation
- Fault injection → verify failsafe behavior
- Process manager: spawn/kill pool, port allocation, crash recovery

### Gateway Tests (ResQ.Mavlink.Gateway.Tests)

- `MessageTranslator`: every mapping, edge cases (zero, max, NaN)
- `GatewayRouter`: routing rules, rate limiting, filtering
- `GcsPassthrough`: bidirectional forwarding, priority conflicts
- End-to-end: MAVLink packet in → protobuf `TelemetryPacket` out

### Dialect Tests (ResQ.Mavlink.Dialect.Tests)

- Round-trip for all ResQ messages
- Codec recognition after dialect registration
- Gateway translation correctness

### Mesh Tests (ResQ.Mavlink.Mesh.Tests)

- Rebroadcast with TTL decrement
- Dedup: same packet from two paths, delivered once
- Priority queuing: emergency ahead of telemetry
- `RadioModel`: range/LOS against known geometries
- Partition/rejoin: buffer during partition, flush on reconnect
- Bounded buffer with priority eviction

### CI Integration

- Unit + codec + transport + gateway + dialect: every PR
- SITL integration: nightly (ArduPilot SITL Docker image)
- Mesh simulation: every PR (no external deps)
