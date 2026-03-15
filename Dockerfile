# Copyright 2026 ResQ
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# ── Stage 1: restore (layer cache for dependencies) ──────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /app

COPY ResQ.Sdk.sln ./
COPY Directory.Build.props Directory.Packages.props global.json ./

# Copy every csproj before copying source so NuGet restore is cached separately
COPY ResQ.Blockchain/*.csproj       ResQ.Blockchain/
COPY ResQ.Clients/*.csproj          ResQ.Clients/
COPY ResQ.Clients.Tests/*.csproj    ResQ.Clients.Tests/
COPY ResQ.Core/*.csproj             ResQ.Core/
COPY ResQ.Protocols/*.csproj        ResQ.Protocols/
COPY ResQ.Simulation/*.csproj       ResQ.Simulation/
COPY ResQ.Storage/*.csproj          ResQ.Storage/
COPY tests/ResQ.Blockchain.Tests/*.csproj  tests/ResQ.Blockchain.Tests/
COPY tests/ResQ.Protocols.Tests/*.csproj   tests/ResQ.Protocols.Tests/
COPY tests/ResQ.Simulation.Tests/*.csproj  tests/ResQ.Simulation.Tests/
COPY tests/ResQ.Storage.Tests/*.csproj     tests/ResQ.Storage.Tests/

RUN dotnet restore

# ── Stage 2: build ───────────────────────────────────────────────────────────
FROM restore AS build
COPY . .
RUN dotnet build -c Release --no-restore

# ── Stage 3: test runner ─────────────────────────────────────────────────────
FROM build AS test
RUN dotnet test -c Release --no-build \
    --logger "console;verbosity=normal" \
    --results-directory /app/TestResults

# ── Stage 4: pack NuGet artifacts ────────────────────────────────────────────
FROM build AS pack
RUN dotnet pack -c Release --no-build --output /app/artifacts

# Default target: run tests
FROM test
