# ResQ .NET SDK — Agent Guide

## Mission
Core .NET libraries and service clients for the ResQ platform. Provides typed API clients, blockchain integration, protocol extensions, simulation utilities, and storage abstractions consumed by ResQ backend services.

## Workspace Layout
- `ResQ.Core/` — Shared models, enums, extensions, telemetry, and service client interfaces.
- `ResQ.Clients/` — Typed HTTP clients: `InfrastructureApiClient`, `CoordinationHceClient`, `BaseServiceClient`.
- `ResQ.Clients.Tests/` — Integration and unit tests for all clients.
- `ResQ.Blockchain/` — Neo blockchain client (`INeoClient`, `NeoClientOptions`).
- `ResQ.Protocols/` — Protobuf-generated types and protocol extensions.
- `ResQ.Simulation/` — Virtual drone and scenario runner for local testing.
- `ResQ.Storage/` — IPFS/Pinata storage client (`IStorageClient`, `PinataClient`).
- `protos/` — Synced local cache of shared `.proto` files authored in `resq-software/resq-proto`.

## Commands
```bash
dotnet build -c Release              # Build all projects
dotnet test -c Release               # Run full test suite
dotnet pack -c Release --no-build    # Produce NuGet packages
dotnet format --verify-no-changes    # Check formatting (CI gate)
./agent-sync.sh --check              # Verify AGENTS.md and CLAUDE.md are in sync
```

## Architecture
- **Target**: .NET 9, `netstandard2.1` compatibility for client libraries.
- **Clients** extend `BaseServiceClient` which handles auth headers, retry, and telemetry.
- **Interfaces** (`INeoClient`, `IStorageClient`) live in `ResQ.Core` — implementations in their respective projects.
- **Protos** are synced from `resq-software/resq-proto` before local builds and then compiled via `protoc`; regenerate with `bash scripts/sync-protos.sh && dotnet build ResQ.Protocols`.
- **NuGet packages** are produced per-project; versioning follows SemVer via `Directory.Build.props`.

## Standards
- All public APIs must have XML doc comments.
- Use `IOptions<T>` for configuration — no raw `IConfiguration` in libraries.
- Nullable reference types enabled globally (`<Nullable>enable</Nullable>`).
- Tests use xUnit; mock with `NSubstitute`; assert with `FluentAssertions`.
- All source files carry the Apache-2.0 license header.
- Keep `AGENTS.md` and `CLAUDE.md` in sync using `./agent-sync.sh`.

## Repository Rules
- Do not commit `bin/` or `obj/` directories.
- Pin package versions in `Directory.Packages.props` — no floating `*` references.
- Breaking API changes require a major version bump.

## References
- [Root README](README.md)
- [Solution File](ResQ.Sdk.sln)
