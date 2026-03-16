<h1 align="center">ResQ .NET SDK</h1>

<p align="center">
  .NET 9 client libraries for integrating with the ResQ autonomous disaster-response platform.
</p>

<p align="center">
  <a href="https://github.com/resq-software/dotnet-sdk/actions/workflows/ci.yml">
    <img src="https://img.shields.io/github/actions/workflow/status/resq-software/dotnet-sdk/ci.yml?branch=main&label=ci&style=flat-square" alt="CI" />
  </a>
  <a href="https://www.nuget.org/packages/ResQ.Core">
    <img src="https://img.shields.io/nuget/v/ResQ.Core?style=flat-square&label=nuget" alt="NuGet" />
  </a>
  <a href="https://codecov.io/gh/resq-software/dotnet-sdk">
    <img src="https://codecov.io/gh/resq-software/dotnet-sdk/graph/badge.svg" alt="Coverage" />
  </a>
  <a href="./LICENSE">
    <img src="https://img.shields.io/badge/license-Apache--2.0-blue.svg?style=flat-square" alt="License: Apache-2.0" />
  </a>
</p>

---

## Table of Contents

- [Overview](#overview)
- [Packages](#packages)
- [Install](#install)
- [Quick Start](#quick-start)
- [Usage](#usage)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [Changelog](#changelog)
- [License](#license)

---

## Overview

The ResQ .NET SDK provides typed client libraries, domain models, and protocol bindings for the [ResQ platform](https://resq.software). It targets .NET 9 and covers the full surface of the ResQ API: blockchain anchoring, drone client abstractions, Protobuf protocol contracts, simulation harnesses, and storage adapters.

**Related projects:**

| Repo | Description |
|------|-------------|
| [resq-software/resQ](https://github.com/resq-software/resQ) | Core platform monorepo |
| [resq-software/cli](https://github.com/resq-software/cli) | CLI tooling |
| [resq-software/mcp](https://github.com/resq-software/mcp) | MCP server |

---

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| `ResQ.Core` | [![NuGet](https://img.shields.io/nuget/v/ResQ.Core?style=flat-square)](https://www.nuget.org/packages/ResQ.Core) | Core domain models — location, enums, blockchain models, shared utilities |
| `ResQ.Blockchain` | [![NuGet](https://img.shields.io/nuget/v/ResQ.Blockchain?style=flat-square)](https://www.nuget.org/packages/ResQ.Blockchain) | Neo N3 blockchain client and IPFS anchoring |
| `ResQ.Clients` | [![NuGet](https://img.shields.io/nuget/v/ResQ.Clients?style=flat-square)](https://www.nuget.org/packages/ResQ.Clients) | Typed HTTP and gRPC clients for ResQ services |
| `ResQ.Protocols` | [![NuGet](https://img.shields.io/nuget/v/ResQ.Protocols?style=flat-square)](https://www.nuget.org/packages/ResQ.Protocols) | Protobuf-generated message types and contracts |
| `ResQ.Simulation` | [![NuGet](https://img.shields.io/nuget/v/ResQ.Simulation?style=flat-square)](https://www.nuget.org/packages/ResQ.Simulation) | PX4 SITL simulation harness and test fixtures |
| `ResQ.Storage` | [![NuGet](https://img.shields.io/nuget/v/ResQ.Storage?style=flat-square)](https://www.nuget.org/packages/ResQ.Storage) | Storage adapters for mission data and telemetry |

---

## Install

Add whichever packages you need:

```sh
dotnet add package ResQ.Core
dotnet add package ResQ.Clients
dotnet add package ResQ.Protocols
```

### From source

```sh
git clone https://github.com/resq-software/dotnet-sdk.git
cd dotnet-sdk
dotnet build -c Release
```

### Docker (build + test)

```sh
docker build -t resq-dotnet-sdk .
# Produce NuGet packages:
docker build --target pack -t resq-dotnet-sdk:pack .
docker run --rm -v $(pwd)/artifacts:/app/artifacts resq-dotnet-sdk:pack
```

### Dev environment (Nix)

```sh
# Install Nix if needed, then:
nix develop        # enters shell with dotnet-sdk 9, protobuf (Linux)
# or:
./scripts/setup.sh # installs Nix + Docker; note: dotnet not in nixpkgs for macOS
```

---

## Quick Start

```csharp
using ResQ.Clients;
using ResQ.Core;

var client = new ResQDroneClient("https://api.resq.software");
var drones = await client.GetActiveDronesAsync();

foreach (var drone in drones)
{
    Console.WriteLine($"{drone.Id}: {drone.Location}");
}
```

---

## Usage

### Domain models (`ResQ.Core`)

```csharp
using ResQ.Core;

var location = new Location(latitude: 37.7749, longitude: -122.4194, altitude: 120.0);
var incident = new IncidentReport(type: IncidentType.Wildfire, location: location);
```

### Blockchain anchoring (`ResQ.Blockchain`)

```csharp
using ResQ.Blockchain;

var neo = new NeoClient(config.NeoRpcUrl);
var txHash = await neo.AnchorMissionAsync(missionId, telemetryHash);
```

### Protobuf contracts (`ResQ.Protocols`)

```csharp
// Generated from the synced protos/ cache — use directly with gRPC channels
using ResQ.Protocols;

var channel = GrpcChannel.ForAddress("https://api.resq.software");
var client  = new DroneService.DroneServiceClient(channel);
var reply   = await client.GetStatusAsync(new DroneStatusRequest { DroneId = "drone-01" });
```

Full API reference: [`docs/`](./docs/)

---

## Configuration

| Variable / Setting | Default | Description |
|--------------------|---------|-------------|
| `RESQ_API_URL` | `https://api.resq.software` | Base URL for ResQ service endpoints |
| `NEO_RPC_URL` | `http://localhost:10332` | Neo N3 RPC endpoint |
| `NEO_MOCK_MODE` | `true` | Use in-memory Neo mock for local development |

Configuration can be provided via environment variables or `appsettings.json`:

```json
{
  "ResQ": {
    "ApiUrl": "https://api.resq.software",
    "NeoMockMode": true
  }
}
```

---

## Contributing

We welcome contributions. Please read [`CONTRIBUTING.md`](./CONTRIBUTING.md) before opening a PR.

**Local setup:**

```sh
git clone https://github.com/resq-software/dotnet-sdk.git
cd dotnet-sdk
./scripts/setup.sh   # installs Nix + Docker; provides dotnet-sdk 9 via nix develop
```

**Run tests:**

```sh
dotnet test -c Release
```

**Regenerate Protobuf bindings** (after updating `proto-source.lock` to a published `resq-proto` revision):

```sh
bash scripts/sync-protos.sh
dotnet build  # MSBuild runs protoc automatically via the .csproj targets
```

**Commit convention:** This project uses [Conventional Commits](https://www.conventionalcommits.org/).
All PRs must follow the `type(scope): subject` format — see the table below.

| Prefix | Effect on version |
|--------|------------------|
| `feat:` | Minor bump (`0.x.0`) |
| `fix:` / `perf:` | Patch bump (`0.0.x`) |
| `BREAKING CHANGE` footer or `!` suffix | Major bump (`x.0.0`) |
| `docs:` `style:` `refactor:` `test:` `chore:` | No version bump |

Releases are driven by git tags — `git tag v1.2.3 && git push --tags` triggers the publish workflow.

---

## Changelog

See [CHANGELOG.md](./CHANGELOG.md) for the full release history.

---

## License

Copyright 2026 ResQ

Licensed under the [Apache License, Version 2.0](./LICENSE).
