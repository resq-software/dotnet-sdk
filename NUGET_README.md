<!--
  Copyright 2026 ResQ

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
-->

# ResQ .NET SDK

ResQ provides .NET 9 packages for integrating with the ResQ autonomous disaster-response platform.

## Packages

- `ResQ.Core`: core domain models and shared utilities
- `ResQ.Clients`: typed HTTP service clients
- `ResQ.Protocols`: protobuf-generated contracts and helpers
- `ResQ.Blockchain`: blockchain integration and mock clients
- `ResQ.Storage`: storage adapters for mission data and telemetry
- `ResQ.Simulation`: simulation harness utilities

## Install

```bash
dotnet add package ResQ.Core
dotnet add package ResQ.Clients
dotnet add package ResQ.Protocols
dotnet add package ResQ.Blockchain
dotnet add package ResQ.Storage
dotnet add package ResQ.Simulation
```

## Source and Docs

- Repository: https://github.com/resq-software/dotnet-sdk
- Platform: https://resq.software

See the repository README for package details, configuration, examples, and contribution guidance.
