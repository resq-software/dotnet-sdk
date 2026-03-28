# MAVLink Phase 1: Core Library + SITL Bridge — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `ResQ.Mavlink` (MAVLink v2 codec, ~25 messages, UDP transport) and `ResQ.Mavlink.Sitl` (ArduPilot SITL bridge integrated with the simulation engine via `IFlightBackend`).

**Architecture:** Two new projects. `ResQ.Mavlink` is a standalone MAVLink v2 library with zero SDK dependencies — codec parses/serializes packets, source-generated message types from `common.xml`, `UdpTransport` implements `IMavlinkTransport`, and `MavlinkConnection` manages heartbeat/command/param protocols. `ResQ.Mavlink.Sitl` bridges ArduPilot SITL processes into the simulation engine via a new `IFlightBackend` abstraction that wraps the existing `IFlightModel` interface.

**Tech Stack:** .NET 9, `netstandard2.1` multi-target for core library, xUnit, FluentAssertions, System.Numerics, `System.IO.Pipelines` for high-perf parsing.

**Spec:** `docs/superpowers/specs/2026-03-27-mavlink-integration-suite-design.md`

---

## File Structure

### ResQ.Mavlink (new project)

```
ResQ.Mavlink/
├── ResQ.Mavlink.csproj
├── Protocol/
│   ├── MavlinkConstants.cs        # Magic bytes, header sizes, version constants
│   ├── MavlinkPacket.cs           # Immutable packet record (header + payload + checksum + signature)
│   ├── MavlinkCodec.cs            # Stateless parse/serialize, CRC-16/MCRF4XX, v2 framing
│   ├── MavlinkCrc.cs              # CRC-16/MCRF4XX implementation + CRC extra lookup
│   └── MavlinkSigning.cs          # SHA-256 truncated-to-48-bit signing (DEFERRED — not in Phase 1 tasks, stub only)
├── Messages/
│   ├── IMavlinkMessage.cs         # Interface: Serialize, Deserialize, MessageId, CrcExtra
│   ├── MessageRegistry.cs         # MessageId → deserializer factory lookup
│   ├── Heartbeat.cs               # Message ID 0
│   ├── SysStatus.cs               # Message ID 1
│   ├── GpsRawInt.cs               # Message ID 24
│   ├── GlobalPositionInt.cs       # Message ID 33
│   ├── Attitude.cs                # Message ID 30
│   ├── VfrHud.cs                  # Message ID 74
│   ├── CommandLong.cs             # Message ID 76
│   ├── CommandAck.cs              # Message ID 77
│   ├── MissionItemInt.cs          # Message ID 73
│   ├── MissionRequest.cs          # Message ID 40 (MissionRequestInt = 51)
│   ├── MissionAck.cs              # Message ID 47
│   ├── MissionCount.cs            # Message ID 44
│   ├── MissionCurrent.cs          # Message ID 42
│   ├── SetMode.cs                 # Message ID 11
│   ├── ParamRequestRead.cs        # Message ID 20
│   ├── ParamValue.cs              # Message ID 22
│   ├── ParamSet.cs                # Message ID 23
│   ├── StatusText.cs              # Message ID 253
│   ├── RcChannelsOverride.cs      # Message ID 70
│   ├── SetPositionTargetGlobalInt.cs # Message ID 86
│   ├── PositionTargetGlobalInt.cs # Message ID 87
│   ├── HomePosition.cs            # Message ID 242
│   └── ExtendedSysState.cs        # Message ID 245
├── Enums/
│   ├── MavType.cs                 # MAV_TYPE enum
│   ├── MavAutopilot.cs            # MAV_AUTOPILOT enum
│   ├── MavModeFlag.cs             # MAV_MODE_FLAG flags
│   ├── MavState.cs                # MAV_STATE enum
│   ├── MavCmd.cs                  # MAV_CMD enum (subset)
│   ├── MavResult.cs               # MAV_RESULT enum
│   ├── MavMissionResult.cs        # MAV_MISSION_RESULT enum
│   ├── MavFrame.cs                # MAV_FRAME enum
│   ├── MavSeverity.cs             # MAV_SEVERITY enum
│   └── GpsFixType.cs              # GPS_FIX_TYPE enum
├── Transport/
│   ├── IMavlinkTransport.cs       # Interface: SendAsync, ReceiveAsync, State, StateChanges
│   ├── TransportState.cs          # Enum: Disconnected, Connecting, Connected, Reconnecting, Disposed
│   ├── TransportDisconnectedException.cs
│   ├── UdpTransport.cs            # UDP transport with broadcast support
│   └── UdpTransportOptions.cs     # Port, broadcast address, buffer sizes
├── Connection/
│   ├── MavlinkConnection.cs       # Heartbeat, command protocol, param protocol, typed streams
│   ├── MavlinkConnectionOptions.cs # Heartbeat interval, command retry, ack timeout
│   └── AsyncEnumerableExtensions.cs # OfType<T>() for IAsyncEnumerable<MavlinkPacket>
└── dialects/
    └── common.xml                 # MAVLink common message definitions (reference, not compiled)
```

### ResQ.Mavlink.Sitl (new project)

```
ResQ.Mavlink.Sitl/
├── ResQ.Mavlink.Sitl.csproj
├── IFlightBackend.cs              # Async flight backend abstraction
├── FlightBackendCapabilities.cs   # Flags: SupportsGps, SupportsFaultInjection, etc.
├── FlightModelBackendAdapter.cs   # Wraps IFlightModel → IFlightBackend
├── ArduPilotSitlBackend.cs        # Manages one SITL process, MAVLink connection
├── SitlBackendOptions.cs          # SITL binary path, vehicle type, physics rate
├── SitlProcessManager.cs          # Pool of SITL processes with port allocation
├── SitlProcessManagerOptions.cs   # Base port, max instances, pool size
├── JsonPhysicsBridge.cs           # Sends sensor data to SITL JSON physics socket at 400Hz
└── SitlTelemetryMapper.cs         # Maps MAVLink telemetry → DronePhysicsState
```

### Test Projects

```
tests/ResQ.Mavlink.Tests/
├── ResQ.Mavlink.Tests.csproj
├── Protocol/
│   ├── MavlinkCrcTests.cs
│   ├── MavlinkCodecTests.cs
│   ├── MavlinkSigningTests.cs
│   └── MavlinkPacketTests.cs
├── Messages/
│   ├── HeartbeatTests.cs
│   ├── GlobalPositionIntTests.cs
│   ├── CommandLongTests.cs
│   ├── MessageRoundTripTests.cs   # Parameterized: all messages
│   └── MessageRegistryTests.cs
├── Transport/
│   ├── UdpTransportTests.cs
│   └── TransportContractTests.cs  # Shared contract, parameterized by transport
└── Connection/
    ├── MavlinkConnectionTests.cs
    └── HeartbeatProtocolTests.cs

tests/ResQ.Mavlink.Sitl.Tests/
├── ResQ.Mavlink.Sitl.Tests.csproj
├── FlightModelBackendAdapterTests.cs
├── SitlTelemetryMapperTests.cs
├── JsonPhysicsBridgeTests.cs
└── Integration/
    └── SitlIntegrationTests.cs    # [Trait("Category", "Integration")]
```

---

## Chunk 1: Project Scaffolding + CRC + Packet

### Task 1: Scaffold ResQ.Mavlink project and test project

**Files:**
- Create: `ResQ.Mavlink/ResQ.Mavlink.csproj`
- Create: `tests/ResQ.Mavlink.Tests/ResQ.Mavlink.Tests.csproj`
- Modify: `ResQ.Sdk.sln`
- Modify: `Directory.Packages.props` (add System.IO.Pipelines if needed)

- [ ] **Step 1: Create ResQ.Mavlink.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net9.0;netstandard2.1</TargetFrameworks>
    <RootNamespace>ResQ.Mavlink</RootNamespace>
    <PackageId>ResQ.Mavlink</PackageId>
    <Version>0.1.0</Version>
    <Authors>ResQ Software</Authors>
    <Description>MAVLink v2 protocol library — codec, messages, and transports for drone communication</Description>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/resq-software/dotnet-sdk</PackageProjectUrl>
    <RepositoryUrl>https://github.com/resq-software/dotnet-sdk</RepositoryUrl>
    <PackageTags>resq;mavlink;drone;uav;protocol</PackageTags>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Options" />
  </ItemGroup>

  <ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.1'">
    <PackageReference Include="System.Memory" />
    <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create test project csproj**

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
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\ResQ.Mavlink\ResQ.Mavlink.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Add packages to Directory.Packages.props**

Add under existing items:

```xml
    <!-- MAVLink -->
    <PackageVersion Include="System.Memory" Version="4.6.0" />
    <PackageVersion Include="Microsoft.Bcl.AsyncInterfaces" Version="9.0.3" />
```

- [ ] **Step 4: Add projects to solution**

Run:
```bash
dotnet sln ResQ.Sdk.sln add ResQ.Mavlink/ResQ.Mavlink.csproj
dotnet sln ResQ.Sdk.sln add tests/ResQ.Mavlink.Tests/ResQ.Mavlink.Tests.csproj --solution-folder tests
```

- [ ] **Step 5: Create directory structure**

```bash
mkdir -p ResQ.Mavlink/{Protocol,Messages,Enums,Transport,Connection,dialects}
mkdir -p tests/ResQ.Mavlink.Tests/{Protocol,Messages,Transport,Connection}
```

- [ ] **Step 6: Verify build**

Run: `dotnet build ResQ.Mavlink/ResQ.Mavlink.csproj`
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add ResQ.Mavlink/ tests/ResQ.Mavlink.Tests/ Directory.Packages.props ResQ.Sdk.sln
git commit -m "feat(mavlink): scaffold ResQ.Mavlink project and test project"
```

---

### Task 2: Implement MavlinkConstants and MavlinkCrc

**Files:**
- Create: `ResQ.Mavlink/Protocol/MavlinkConstants.cs`
- Create: `ResQ.Mavlink/Protocol/MavlinkCrc.cs`
- Create: `tests/ResQ.Mavlink.Tests/Protocol/MavlinkCrcTests.cs`

- [ ] **Step 1: Write CRC tests**

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

using FluentAssertions;
using ResQ.Mavlink.Protocol;

namespace ResQ.Mavlink.Tests.Protocol;

public class MavlinkCrcTests
{
    [Fact]
    public void Accumulate_EmptyInput_ReturnsInitialSeed()
    {
        var crc = MavlinkCrc.Calculate(ReadOnlySpan<byte>.Empty);
        // CRC-16/MCRF4XX initial value 0xFFFF with no data
        crc.Should().Be(0xFFFF);
    }

    [Fact]
    public void Calculate_KnownHeartbeatPayload_MatchesExpected()
    {
        // A known MAVLink v2 heartbeat payload (9 bytes):
        // custom_mode=0, type=2(quadrotor), autopilot=3(ardupilot),
        // base_mode=81, system_status=4(active), mavlink_version=3
        byte[] payload = [0x00, 0x00, 0x00, 0x00, 0x02, 0x03, 0x51, 0x04, 0x03];
        byte crcExtra = 50; // Heartbeat CRC_EXTRA

        var crc = MavlinkCrc.Calculate(payload);
        crc = MavlinkCrc.Accumulate(crc, crcExtra);

        // The CRC must be deterministic and non-zero
        crc.Should().NotBe(0);
        crc.Should().NotBe(0xFFFF);
    }

    [Fact]
    public void Accumulate_SingleByte_ProducesConsistentResult()
    {
        var crc1 = MavlinkCrc.Accumulate(0xFFFF, 0x42);
        var crc2 = MavlinkCrc.Accumulate(0xFFFF, 0x42);
        crc1.Should().Be(crc2);
    }

    [Fact]
    public void GetCrcExtra_Heartbeat_Returns50()
    {
        MavlinkCrc.GetCrcExtra(0).Should().Be(50);
    }

    [Fact]
    public void GetCrcExtra_UnknownMessageId_ReturnsNull()
    {
        MavlinkCrc.GetCrcExtra(99999).Should().BeNull();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ --filter "FullyQualifiedName~MavlinkCrcTests" -v n`
Expected: FAIL — types do not exist yet.

- [ ] **Step 3: Implement MavlinkConstants**

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

namespace ResQ.Mavlink.Protocol;

/// <summary>
/// MAVLink v2 protocol constants.
/// </summary>
public static class MavlinkConstants
{
    /// <summary>MAVLink v2 start-of-frame marker.</summary>
    public const byte StxV2 = 0xFD;

    /// <summary>MAVLink v1 start-of-frame marker (for detection/rejection).</summary>
    public const byte StxV1 = 0xFE;

    /// <summary>MAVLink v2 header length in bytes (excluding STX).</summary>
    public const int HeaderLength = 9;

    /// <summary>CRC length in bytes (CRC-16 = 2 bytes).</summary>
    public const int CrcLength = 2;

    /// <summary>Signature length when present (link ID + timestamp + signature bytes).</summary>
    public const int SignatureLength = 13;

    /// <summary>Maximum payload length for MAVLink v2.</summary>
    public const int MaxPayloadLength = 255;

    /// <summary>Minimum packet size: STX + header + CRC (no payload).</summary>
    public const int MinPacketLength = 1 + HeaderLength + CrcLength;

    /// <summary>Incompatibility flag indicating the packet is signed.</summary>
    public const byte IncompatFlagSigned = 0x01;
}
```

- [ ] **Step 4: Implement MavlinkCrc**

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

namespace ResQ.Mavlink.Protocol;

/// <summary>
/// CRC-16/MCRF4XX used by MAVLink for packet integrity, plus CRC extra seed lookup.
/// </summary>
public static class MavlinkCrc
{
    private const ushort InitialValue = 0xFFFF;

    // CRC extra seeds for common.xml messages (message_id → crc_extra).
    // Sourced from MAVLink common.xml message definitions. Thread-safe for dialect registration.
    private static readonly ConcurrentDictionary<uint, byte> CrcExtraTable = new(
        new Dictionary<uint, byte>()
    {
        [0] = 50,     // HEARTBEAT
        [1] = 124,    // SYS_STATUS
        [11] = 89,    // SET_MODE
        [20] = 214,   // PARAM_REQUEST_READ
        [22] = 220,   // PARAM_VALUE
        [23] = 168,   // PARAM_SET
        [24] = 24,    // GPS_RAW_INT
        [30] = 39,    // ATTITUDE
        [33] = 104,   // GLOBAL_POSITION_INT
        [40] = 230,   // MISSION_REQUEST
        [42] = 28,    // MISSION_CURRENT
        [44] = 221,   // MISSION_COUNT
        [47] = 153,   // MISSION_ACK
        [51] = 196,   // MISSION_REQUEST_INT
        [70] = 124,   // RC_CHANNELS_OVERRIDE
        [73] = 38,    // MISSION_ITEM_INT
        [74] = 20,    // VFR_HUD
        [76] = 152,   // COMMAND_LONG
        [77] = 143,   // COMMAND_ACK
        [86] = 5,     // SET_POSITION_TARGET_GLOBAL_INT
        [87] = 150,   // POSITION_TARGET_GLOBAL_INT
        [242] = 104,  // HOME_POSITION
        [245] = 130,  // EXTENDED_SYS_STATE
        [253] = 83,   // STATUSTEXT
    });

    /// <summary>
    /// Computes the CRC-16/MCRF4XX over <paramref name="data"/>.
    /// </summary>
    public static ushort Calculate(ReadOnlySpan<byte> data)
    {
        var crc = InitialValue;
        for (var i = 0; i < data.Length; i++)
            crc = Accumulate(crc, data[i]);
        return crc;
    }

    /// <summary>
    /// Feeds a single byte into an ongoing CRC calculation.
    /// </summary>
    public static ushort Accumulate(ushort crc, byte value)
    {
        var tmp = (byte)(value ^ (byte)(crc & 0xFF));
        tmp ^= (byte)(tmp << 4);
        return (ushort)((crc >> 8) ^ (tmp << 8) ^ (tmp << 3) ^ (tmp >> 4));
    }

    /// <summary>
    /// Returns the CRC extra seed for a given message ID, or null if unknown.
    /// </summary>
    public static byte? GetCrcExtra(uint messageId) =>
        CrcExtraTable.TryGetValue(messageId, out var extra) ? extra : null;

    /// <summary>
    /// Registers a CRC extra for a custom dialect message. Used by dialect extensions.
    /// </summary>
    public static void RegisterCrcExtra(uint messageId, byte crcExtra) =>
        CrcExtraTable[messageId] = crcExtra;
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ --filter "FullyQualifiedName~MavlinkCrcTests" -v n`
Expected: All 5 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add ResQ.Mavlink/Protocol/ tests/ResQ.Mavlink.Tests/Protocol/MavlinkCrcTests.cs
git commit -m "feat(mavlink): implement CRC-16/MCRF4XX and protocol constants"
```

---

### Task 3: Implement MavlinkPacket

**Files:**
- Create: `ResQ.Mavlink/Protocol/MavlinkPacket.cs`
- Create: `tests/ResQ.Mavlink.Tests/Protocol/MavlinkPacketTests.cs`

- [ ] **Step 1: Write packet tests**

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

using FluentAssertions;
using ResQ.Mavlink.Protocol;

namespace ResQ.Mavlink.Tests.Protocol;

public class MavlinkPacketTests
{
    [Fact]
    public void Constructor_SetsAllFields()
    {
        byte[] payload = [0x01, 0x02, 0x03];
        var packet = new MavlinkPacket(
            sequenceNumber: 42,
            systemId: 1,
            componentId: 1,
            messageId: 0,
            payload: payload,
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

        packet.SequenceNumber.Should().Be(42);
        packet.SystemId.Should().Be(1);
        packet.ComponentId.Should().Be(1);
        packet.MessageId.Should().Be(0u);
        packet.Payload.Should().BeEquivalentTo(payload);
        packet.IsSigned.Should().BeFalse();
    }

    [Fact]
    public void IsSigned_WithSignature_ReturnsTrue()
    {
        var packet = new MavlinkPacket(0, 1, 1, 0, [],
            incompatFlags: MavlinkConstants.IncompatFlagSigned,
            compatFlags: 0,
            signature: new byte[MavlinkConstants.SignatureLength]);

        packet.IsSigned.Should().BeTrue();
    }

    [Fact]
    public void PayloadLength_ReturnsCorrectLength()
    {
        byte[] payload = [0x01, 0x02, 0x03, 0x04, 0x05];
        var packet = new MavlinkPacket(0, 1, 1, 33, payload, 0, 0, null);
        packet.PayloadLength.Should().Be(5);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ --filter "FullyQualifiedName~MavlinkPacketTests" -v n`
Expected: FAIL — `MavlinkPacket` does not exist.

- [ ] **Step 3: Implement MavlinkPacket**

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

namespace ResQ.Mavlink.Protocol;

/// <summary>
/// An immutable representation of a parsed MAVLink v2 packet.
/// </summary>
/// <param name="SequenceNumber">Packet sequence number (0-255, wrapping).</param>
/// <param name="SystemId">Sending system ID (1-255).</param>
/// <param name="ComponentId">Sending component ID (1-255).</param>
/// <param name="MessageId">24-bit message ID.</param>
/// <param name="Payload">Raw message payload bytes (zero-copy via Memory).</param>
/// <param name="IncompatFlags">Incompatibility flags.</param>
/// <param name="CompatFlags">Compatibility flags.</param>
/// <param name="Signature">Optional 13-byte signature (null if unsigned).</param>
public sealed record MavlinkPacket(
    byte SequenceNumber,
    byte SystemId,
    byte ComponentId,
    uint MessageId,
    ReadOnlyMemory<byte> Payload,
    byte IncompatFlags,
    byte CompatFlags,
    ReadOnlyMemory<byte>? Signature)
{
    /// <summary>Gets the payload length.</summary>
    public int PayloadLength => Payload.Length;

    /// <summary>Gets whether this packet has a signature.</summary>
    public bool IsSigned => (IncompatFlags & MavlinkConstants.IncompatFlagSigned) != 0
                            && Signature is not null;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ --filter "FullyQualifiedName~MavlinkPacketTests" -v n`
Expected: All 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add ResQ.Mavlink/Protocol/MavlinkPacket.cs tests/ResQ.Mavlink.Tests/Protocol/MavlinkPacketTests.cs
git commit -m "feat(mavlink): implement MavlinkPacket record"
```

---

### Task 4: Implement MavlinkCodec (parse + serialize)

**Files:**
- Create: `ResQ.Mavlink/Protocol/MavlinkCodec.cs`
- Create: `tests/ResQ.Mavlink.Tests/Protocol/MavlinkCodecTests.cs`

- [ ] **Step 1: Write codec tests**

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

using FluentAssertions;
using ResQ.Mavlink.Protocol;

namespace ResQ.Mavlink.Tests.Protocol;

public class MavlinkCodecTests
{
    [Fact]
    public void Serialize_ThenParse_RoundTrips()
    {
        byte[] payload = [0x00, 0x00, 0x00, 0x00, 0x02, 0x03, 0x51, 0x04, 0x03];
        var original = new MavlinkPacket(
            sequenceNumber: 1,
            systemId: 1,
            componentId: 1,
            messageId: 0, // HEARTBEAT
            payload: payload,
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

        var bytes = MavlinkCodec.Serialize(original);
        var parsed = MavlinkCodec.TryParse(bytes, out var result);

        parsed.Should().BeTrue();
        result.Should().NotBeNull();
        result!.SystemId.Should().Be(1);
        result.ComponentId.Should().Be(1);
        result.MessageId.Should().Be(0u);
        result.Payload.Should().BeEquivalentTo(payload);
        result.SequenceNumber.Should().Be(1);
    }

    [Fact]
    public void TryParse_TooShortBuffer_ReturnsFalse()
    {
        byte[] tooShort = [MavlinkConstants.StxV2, 0x00, 0x00];
        MavlinkCodec.TryParse(tooShort, out var result).Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryParse_WrongStx_ReturnsFalse()
    {
        var bytes = new byte[MavlinkConstants.MinPacketLength];
        bytes[0] = 0xAA; // wrong STX
        MavlinkCodec.TryParse(bytes, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_CorruptedCrc_ReturnsFalse()
    {
        byte[] payload = [0x00, 0x00, 0x00, 0x00, 0x02, 0x03, 0x51, 0x04, 0x03];
        var original = new MavlinkPacket(1, 1, 1, 0, payload, 0, 0, null);
        var bytes = MavlinkCodec.Serialize(original);

        // Corrupt the last byte (CRC)
        bytes[^1] ^= 0xFF;

        MavlinkCodec.TryParse(bytes, out _).Should().BeFalse();
    }

    [Fact]
    public void Serialize_SetsStxByte()
    {
        var packet = new MavlinkPacket(0, 1, 1, 0, [0x00], 0, 0, null);
        var bytes = MavlinkCodec.Serialize(packet);
        bytes[0].Should().Be(MavlinkConstants.StxV2);
    }

    [Fact]
    public void Serialize_PayloadLength_EncodedInHeader()
    {
        byte[] payload = [0x01, 0x02, 0x03, 0x04, 0x05];
        var packet = new MavlinkPacket(0, 1, 1, 33, payload, 0, 0, null);
        var bytes = MavlinkCodec.Serialize(packet);
        bytes[1].Should().Be(5); // payload length byte
    }

    [Fact]
    public void Serialize_UnknownMessageId_Throws()
    {
        byte[] payload = [0x01, 0x02];
        var packet = new MavlinkPacket(0, 1, 1, 59999, payload, 0, 0, null);
        var act = () => MavlinkCodec.Serialize(packet);
        act.Should().Throw<InvalidOperationException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ --filter "FullyQualifiedName~MavlinkCodecTests" -v n`
Expected: FAIL — `MavlinkCodec` does not exist.

- [ ] **Step 3: Implement MavlinkCodec**

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

namespace ResQ.Mavlink.Protocol;

/// <summary>
/// Stateless MAVLink v2 packet serializer and parser.
/// </summary>
public static class MavlinkCodec
{
    /// <summary>
    /// Serializes a <see cref="MavlinkPacket"/> into a byte array with proper framing and CRC.
    /// </summary>
    /// <param name="packet">The packet to serialize.</param>
    /// <returns>The framed wire bytes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CRC extra is not known for the message ID.</exception>
    public static byte[] Serialize(MavlinkPacket packet)
    {
        var crcExtra = MavlinkCrc.GetCrcExtra(packet.MessageId)
            ?? throw new InvalidOperationException(
                $"No CRC extra registered for message ID {packet.MessageId}. Register via MavlinkCrc.RegisterCrcExtra().");

        var payloadLen = packet.Payload.Length;
        var totalLen = 1 + MavlinkConstants.HeaderLength + payloadLen + MavlinkConstants.CrcLength;
        if (packet.IsSigned)
            totalLen += MavlinkConstants.SignatureLength;

        var buffer = new byte[totalLen];
        var offset = 0;

        // STX
        buffer[offset++] = MavlinkConstants.StxV2;

        // Header (9 bytes)
        buffer[offset++] = (byte)payloadLen;
        buffer[offset++] = packet.IncompatFlags;
        buffer[offset++] = packet.CompatFlags;
        buffer[offset++] = packet.SequenceNumber;
        buffer[offset++] = packet.SystemId;
        buffer[offset++] = packet.ComponentId;
        buffer[offset++] = (byte)(packet.MessageId & 0xFF);
        buffer[offset++] = (byte)((packet.MessageId >> 8) & 0xFF);
        buffer[offset++] = (byte)((packet.MessageId >> 16) & 0xFF);

        // Payload
        Array.Copy(packet.Payload, 0, buffer, offset, payloadLen);
        offset += payloadLen;

        // CRC over header (bytes 1..9) + payload + crc_extra
        // CRC is computed over bytes[1] through bytes[offset-1] then crc_extra
        var crcSpan = buffer.AsSpan(1, MavlinkConstants.HeaderLength + payloadLen);
        var crc = MavlinkCrc.Calculate(crcSpan);
        crc = MavlinkCrc.Accumulate(crc, crcExtra);

        buffer[offset++] = (byte)(crc & 0xFF);
        buffer[offset++] = (byte)(crc >> 8);

        // Signature (if present)
        if (packet.IsSigned && packet.Signature is not null)
        {
            Array.Copy(packet.Signature, 0, buffer, offset, MavlinkConstants.SignatureLength);
        }

        return buffer;
    }

    /// <summary>
    /// Attempts to parse a MAVLink v2 packet from <paramref name="data"/>.
    /// </summary>
    /// <param name="data">Raw bytes starting with STX.</param>
    /// <param name="packet">The parsed packet, or null on failure.</param>
    /// <returns><c>true</c> if parsing succeeded; <c>false</c> otherwise.</returns>
    public static bool TryParse(ReadOnlySpan<byte> data, out MavlinkPacket? packet)
    {
        packet = null;

        if (data.Length < MavlinkConstants.MinPacketLength)
            return false;

        if (data[0] != MavlinkConstants.StxV2)
            return false;

        var payloadLen = data[1];
        var incompatFlags = data[2];
        var compatFlags = data[3];
        var seq = data[4];
        var sysId = data[5];
        var compId = data[6];
        var msgId = (uint)(data[7] | (data[8] << 8) | (data[9] << 16));

        var expectedLen = 1 + MavlinkConstants.HeaderLength + payloadLen + MavlinkConstants.CrcLength;
        var isSigned = (incompatFlags & MavlinkConstants.IncompatFlagSigned) != 0;
        if (isSigned)
            expectedLen += MavlinkConstants.SignatureLength;

        if (data.Length < expectedLen)
            return false;

        // Verify CRC
        var crcExtra = MavlinkCrc.GetCrcExtra(msgId);
        if (crcExtra is null)
            return false; // Unknown message — can't verify CRC

        var crcSpan = data.Slice(1, MavlinkConstants.HeaderLength + payloadLen);
        var crc = MavlinkCrc.Calculate(crcSpan);
        crc = MavlinkCrc.Accumulate(crc, crcExtra.Value);

        var crcOffset = 1 + MavlinkConstants.HeaderLength + payloadLen;
        var wireCrc = (ushort)(data[crcOffset] | (data[crcOffset + 1] << 8));

        if (crc != wireCrc)
            return false;

        // Extract payload
        var payload = data.Slice(1 + MavlinkConstants.HeaderLength, payloadLen).ToArray();

        // Extract signature if present
        byte[]? signature = null;
        if (isSigned)
        {
            signature = data.Slice(crcOffset + MavlinkConstants.CrcLength, MavlinkConstants.SignatureLength).ToArray();
        }

        packet = new MavlinkPacket(seq, sysId, compId, msgId, payload, incompatFlags, compatFlags, signature);
        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ --filter "FullyQualifiedName~MavlinkCodecTests" -v n`
Expected: All 6 tests PASS.

- [ ] **Step 5: Add fuzz test — random bytes must never throw**

Add this test to `MavlinkCodecTests.cs`:

```csharp
    [Fact]
    public void TryParse_RandomBytes_NeverThrows()
    {
        var rng = new Random(42);
        for (var i = 0; i < 1000; i++)
        {
            var len = rng.Next(0, 300);
            var bytes = new byte[len];
            rng.NextBytes(bytes);

            // Must never throw — only return true/false
            var act = () => MavlinkCodec.TryParse(bytes, out _);
            act.Should().NotThrow();
        }
    }
```

- [ ] **Step 6: Run full test suite**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ -v n`
Expected: All tests PASS.

- [ ] **Step 7: Commit**

```bash
git add ResQ.Mavlink/Protocol/MavlinkCodec.cs tests/ResQ.Mavlink.Tests/Protocol/MavlinkCodecTests.cs
git commit -m "feat(mavlink): implement MAVLink v2 codec — serialize and parse with CRC validation"
```

---

## Chunk 2: Enums + Messages + Message Registry

### Task 5: Implement MAVLink enums

**Files:**
- Create: `ResQ.Mavlink/Enums/MavType.cs`
- Create: `ResQ.Mavlink/Enums/MavAutopilot.cs`
- Create: `ResQ.Mavlink/Enums/MavModeFlag.cs`
- Create: `ResQ.Mavlink/Enums/MavState.cs`
- Create: `ResQ.Mavlink/Enums/MavCmd.cs`
- Create: `ResQ.Mavlink/Enums/MavResult.cs`
- Create: `ResQ.Mavlink/Enums/MavMissionResult.cs`
- Create: `ResQ.Mavlink/Enums/MavFrame.cs`
- Create: `ResQ.Mavlink/Enums/MavSeverity.cs`
- Create: `ResQ.Mavlink/Enums/GpsFixType.cs`

These are pure data enums extracted from the MAVLink `common.xml`. No tests needed — they're just constants.

- [ ] **Step 1: Create all enum files**

Each enum follows this pattern (showing MavType as example):

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

namespace ResQ.Mavlink.Enums;

/// <summary>
/// MAVLINK component type (MAV_TYPE). Identifies what kind of vehicle/component this is.
/// </summary>
public enum MavType : byte
{
    Generic = 0,
    FixedWing = 1,
    Quadrotor = 2,
    Coaxial = 3,
    Helicopter = 4,
    Hexarotor = 13,
    Octorotor = 14,
    Tricopter = 15,
    GroundRover = 10,
    Submarine = 12,
    Gcs = 6,
}
```

Create all 10 enum files following MAVLink `common.xml` definitions. Include only the values needed for the Phase 1 message set. `MavCmd` should include:
- `ComponentArmDisarm = 400`
- `NavTakeoff = 22`
- `NavLand = 21`
- `NavReturnToLaunch = 20`
- `NavWaypoint = 16`
- `DoSetMode = 176`
- `MissionStart = 300`

`MavModeFlag` should be `[Flags]` attributed.

- [ ] **Step 2: Verify build**

Run: `dotnet build ResQ.Mavlink/ResQ.Mavlink.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add ResQ.Mavlink/Enums/
git commit -m "feat(mavlink): add MAVLink common enum definitions"
```

---

### Task 6: Implement IMavlinkMessage and first messages (Heartbeat, Attitude, GlobalPositionInt)

**Files:**
- Create: `ResQ.Mavlink/Messages/IMavlinkMessage.cs`
- Create: `ResQ.Mavlink/Messages/Heartbeat.cs`
- Create: `ResQ.Mavlink/Messages/Attitude.cs`
- Create: `ResQ.Mavlink/Messages/GlobalPositionInt.cs`
- Create: `tests/ResQ.Mavlink.Tests/Messages/HeartbeatTests.cs`
- Create: `tests/ResQ.Mavlink.Tests/Messages/GlobalPositionIntTests.cs`

- [ ] **Step 1: Write Heartbeat and GlobalPositionInt tests**

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

using FluentAssertions;
using ResQ.Mavlink.Enums;
using ResQ.Mavlink.Messages;

namespace ResQ.Mavlink.Tests.Messages;

public class HeartbeatTests
{
    [Fact]
    public void Serialize_ThenDeserialize_RoundTrips()
    {
        var original = new Heartbeat
        {
            Type = MavType.Quadrotor,
            Autopilot = MavAutopilot.ArduPilotMega,
            BaseMode = MavModeFlag.CustomModeEnabled | MavModeFlag.SafetyArmed,
            CustomMode = 5,
            SystemStatus = MavState.Active,
            MavlinkVersion = 3,
        };

        Span<byte> buffer = stackalloc byte[Heartbeat.PayloadSize];
        original.Serialize(buffer);

        var parsed = Heartbeat.Deserialize(buffer);
        parsed.Type.Should().Be(MavType.Quadrotor);
        parsed.Autopilot.Should().Be(MavAutopilot.ArduPilotMega);
        parsed.CustomMode.Should().Be(5u);
        parsed.SystemStatus.Should().Be(MavState.Active);
        parsed.MavlinkVersion.Should().Be(3);
    }

    [Fact]
    public void MessageId_IsZero()
    {
        var hb = new Heartbeat();
        hb.MessageId.Should().Be(0u);
    }
}
```

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

using FluentAssertions;
using ResQ.Mavlink.Messages;

namespace ResQ.Mavlink.Tests.Messages;

public class GlobalPositionIntTests
{
    [Fact]
    public void Serialize_ThenDeserialize_RoundTrips()
    {
        var original = new GlobalPositionInt
        {
            TimeBootMs = 12345,
            Lat = 473977418,    // 47.3977418° (Zurich)
            Lon = 85255792,     // 8.5255792°
            Alt = 408000,       // 408m in mm
            RelativeAlt = 50000, // 50m in mm
            Vx = 100,           // 1.0 m/s (cm/s)
            Vy = -50,
            Vz = 10,
            Hdg = 18000,        // 180.00°
        };

        Span<byte> buffer = stackalloc byte[GlobalPositionInt.PayloadSize];
        original.Serialize(buffer);

        var parsed = GlobalPositionInt.Deserialize(buffer);
        parsed.Lat.Should().Be(473977418);
        parsed.Lon.Should().Be(85255792);
        parsed.Alt.Should().Be(408000);
        parsed.RelativeAlt.Should().Be(50000);
        parsed.Vx.Should().Be(100);
        parsed.Vy.Should().Be(-50);
        parsed.Hdg.Should().Be(18000);
    }

    [Fact]
    public void MessageId_Is33()
    {
        var msg = new GlobalPositionInt();
        msg.MessageId.Should().Be(33u);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ --filter "FullyQualifiedName~HeartbeatTests|FullyQualifiedName~GlobalPositionIntTests" -v n`
Expected: FAIL.

- [ ] **Step 3: Implement IMavlinkMessage**

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

namespace ResQ.Mavlink.Messages;

/// <summary>
/// Contract for a strongly-typed MAVLink message that can serialize/deserialize its payload.
/// </summary>
public interface IMavlinkMessage
{
    /// <summary>Gets the MAVLink message ID.</summary>
    uint MessageId { get; }

    /// <summary>Gets the CRC extra seed for this message type.</summary>
    byte CrcExtra { get; }

    /// <summary>Serializes this message into <paramref name="buffer"/>.</summary>
    void Serialize(Span<byte> buffer);
}
```

- [ ] **Step 4: Implement Heartbeat**

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

using System.Buffers.Binary;
using ResQ.Mavlink.Enums;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink HEARTBEAT message (ID 0). Sent at 1 Hz to indicate system is alive.
/// </summary>
public readonly record struct Heartbeat : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 9;

    public uint CustomMode { get; set; }
    public MavType Type { get; set; }
    public MavAutopilot Autopilot { get; set; }
    public MavModeFlag BaseMode { get; set; }
    public MavState SystemStatus { get; set; }
    public byte MavlinkVersion { get; set; }

    public readonly uint MessageId => 0;
    public readonly byte CrcExtra => 50;

    public readonly void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, CustomMode);
        buffer[4] = (byte)Type;
        buffer[5] = (byte)Autopilot;
        buffer[6] = (byte)BaseMode;
        buffer[7] = (byte)SystemStatus;
        buffer[8] = MavlinkVersion;
    }

    public static Heartbeat Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new Heartbeat
        {
            CustomMode = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Type = (MavType)buffer[4],
            Autopilot = (MavAutopilot)buffer[5],
            BaseMode = (MavModeFlag)buffer[6],
            SystemStatus = (MavState)buffer[7],
            MavlinkVersion = buffer[8],
        };
    }
}
```

- [ ] **Step 5: Implement GlobalPositionInt**

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

using System.Buffers.Binary;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink GLOBAL_POSITION_INT message (ID 33). Filtered GPS position.
/// Lat/Lon in degE7, Alt in mm, velocities in cm/s, heading in cdeg.
/// </summary>
public readonly record struct GlobalPositionInt : IMavlinkMessage
{
    public const int PayloadSize = 28;

    public uint TimeBootMs { get; set; }
    public int Lat { get; set; }
    public int Lon { get; set; }
    public int Alt { get; set; }
    public int RelativeAlt { get; set; }
    public short Vx { get; set; }
    public short Vy { get; set; }
    public short Vz { get; set; }
    public ushort Hdg { get; set; }

    public readonly uint MessageId => 33;
    public readonly byte CrcExtra => 104;

    public readonly void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], Lat);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], Lon);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], Alt);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], RelativeAlt);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[20..], Vx);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[22..], Vy);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[24..], Vz);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[26..], Hdg);
    }

    public static GlobalPositionInt Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new GlobalPositionInt
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Lat = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
            Lon = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
            Alt = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
            RelativeAlt = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]),
            Vx = BinaryPrimitives.ReadInt16LittleEndian(buffer[20..]),
            Vy = BinaryPrimitives.ReadInt16LittleEndian(buffer[22..]),
            Vz = BinaryPrimitives.ReadInt16LittleEndian(buffer[24..]),
            Hdg = BinaryPrimitives.ReadUInt16LittleEndian(buffer[26..]),
        };
    }
}
```

- [ ] **Step 6: Implement Attitude (same pattern)**

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

using System.Buffers.Binary;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink ATTITUDE message (ID 30). Roll/pitch/yaw in radians, rates in rad/s.
/// </summary>
public readonly record struct Attitude : IMavlinkMessage
{
    public const int PayloadSize = 28;

    public uint TimeBootMs { get; set; }
    public float Roll { get; set; }
    public float Pitch { get; set; }
    public float Yaw { get; set; }
    public float Rollspeed { get; set; }
    public float Pitchspeed { get; set; }
    public float Yawspeed { get; set; }

    public readonly uint MessageId => 30;
    public readonly byte CrcExtra => 39;

    public readonly void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Roll);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Pitch);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Yaw);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Rollspeed);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], Pitchspeed);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Yawspeed);
    }

    public static Attitude Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new Attitude
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Roll = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            Pitch = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Yaw = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Rollspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Pitchspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            Yawspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
        };
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ --filter "FullyQualifiedName~HeartbeatTests|FullyQualifiedName~GlobalPositionIntTests" -v n`
Expected: All tests PASS.

- [ ] **Step 8: Commit**

```bash
git add ResQ.Mavlink/Messages/ tests/ResQ.Mavlink.Tests/Messages/
git commit -m "feat(mavlink): implement IMavlinkMessage, Heartbeat, Attitude, GlobalPositionInt"
```

---

### Task 7: Implement remaining Phase 1 messages

**Files:**
- Create: All remaining message files in `ResQ.Mavlink/Messages/` (SysStatus, GpsRawInt, VfrHud, CommandLong, CommandAck, MissionItemInt, MissionRequest, MissionAck, MissionCount, MissionCurrent, SetMode, ParamRequestRead, ParamValue, ParamSet, StatusText, RcChannelsOverride, SetPositionTargetGlobalInt, PositionTargetGlobalInt, HomePosition, ExtendedSysState)
- Create: `tests/ResQ.Mavlink.Tests/Messages/MessageRoundTripTests.cs`

Each message follows the exact same pattern as Heartbeat/GlobalPositionInt/Attitude: struct implementing `IMavlinkMessage` with `Serialize(Span<byte>)` and `static Deserialize(ReadOnlySpan<byte>)`. All fields use little-endian `BinaryPrimitives`. Refer to MAVLink `common.xml` for exact field order and types.

- [ ] **Step 1: Write parameterized round-trip test**

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

using FluentAssertions;
using ResQ.Mavlink.Enums;
using ResQ.Mavlink.Messages;

namespace ResQ.Mavlink.Tests.Messages;

public class MessageRoundTripTests
{
    [Fact]
    public void CommandLong_RoundTrips()
    {
        var original = new CommandLong
        {
            TargetSystem = 1,
            TargetComponent = 1,
            Command = MavCmd.ComponentArmDisarm,
            Confirmation = 0,
            Param1 = 1.0f, // arm
        };

        Span<byte> buf = stackalloc byte[CommandLong.PayloadSize];
        original.Serialize(buf);
        var parsed = CommandLong.Deserialize(buf);

        parsed.Command.Should().Be(MavCmd.ComponentArmDisarm);
        parsed.Param1.Should().Be(1.0f);
        parsed.TargetSystem.Should().Be(1);
    }

    [Fact]
    public void CommandAck_RoundTrips()
    {
        var original = new CommandAck
        {
            Command = MavCmd.ComponentArmDisarm,
            Result = MavResult.Accepted,
        };

        Span<byte> buf = stackalloc byte[CommandAck.PayloadSize];
        original.Serialize(buf);
        var parsed = CommandAck.Deserialize(buf);

        parsed.Command.Should().Be(MavCmd.ComponentArmDisarm);
        parsed.Result.Should().Be(MavResult.Accepted);
    }

    [Fact]
    public void StatusText_RoundTrips()
    {
        var original = new StatusText
        {
            Severity = MavSeverity.Info,
            Text = "PreArm: Ready to arm",
        };

        Span<byte> buf = stackalloc byte[StatusText.PayloadSize];
        original.Serialize(buf);
        var parsed = StatusText.Deserialize(buf);

        parsed.Severity.Should().Be(MavSeverity.Info);
        parsed.Text.Should().StartWith("PreArm: Ready to arm");
    }

    [Fact]
    public void ParamValue_RoundTrips()
    {
        var original = new ParamValue
        {
            ParamId = "ARMING_CHECK",
            ParamValue_ = 1.0f,
            ParamType = 9, // REAL32
            ParamCount = 500,
            ParamIndex = 42,
        };

        Span<byte> buf = stackalloc byte[ParamValue.PayloadSize];
        original.Serialize(buf);
        var parsed = ParamValue.Deserialize(buf);

        parsed.ParamId.Should().Be("ARMING_CHECK");
        parsed.ParamValue_.Should().Be(1.0f);
        parsed.ParamIndex.Should().Be(42);
    }
}
```

- [ ] **Step 2: Implement all remaining messages**

Follow the Heartbeat pattern for each. Key messages and their payload structures:

- `CommandLong` (ID 76, 33 bytes): param1-7 (float), command (MavCmd/ushort), target_system, target_component, confirmation
- `CommandAck` (ID 77, 3 bytes): command (ushort), result (MavResult/byte)
- `StatusText` (ID 253, 51 bytes): severity (byte), text (char[50] null-terminated)
- `ParamValue` (ID 22, 25 bytes): param_value (float), param_count (ushort), param_index (ushort), param_id (char[16]), param_type (byte)
- `ParamSet` (ID 23, 23 bytes): param_value (float), target_system, target_component, param_id (char[16]), param_type (byte)
- `ParamRequestRead` (ID 20, 20 bytes): param_index (short), target_system, target_component, param_id (char[16])
- `SetMode` (ID 11, 6 bytes): custom_mode (uint), target_system, base_mode
- `SysStatus` (ID 1, 31 bytes): sensors fields (uint32s), load, voltage, current, battery, drop rates, errors
- `GpsRawInt` (ID 24, 30 bytes): time_usec, lat, lon, alt, eph, epv, vel, cog, fix_type, satellites
- `VfrHud` (ID 74, 20 bytes): airspeed, groundspeed, heading, throttle, alt, climb (floats/ints)
- `MissionCount` (ID 44, 4 bytes): count (ushort), target_system, target_component
- `MissionCurrent` (ID 42, 2 bytes): seq (ushort)
- `MissionAck` (ID 47, 3 bytes): target_system, target_component, type (MavMissionResult)
- `MissionItemInt` (ID 73, 37 bytes): param1-4 (float), x (int32 degE7), y (int32 degE7), z (float), seq, command, target_system, target_component, frame, current, autocontinue
- `MissionRequest` (ID 40, 4 bytes): seq (ushort), target_system, target_component
- `RcChannelsOverride` (ID 70, 18 bytes): chan1-8_raw (ushort), target_system, target_component
- `SetPositionTargetGlobalInt` (ID 86, 53 bytes): time_boot_ms, lat_int, lon_int, alt, vx, vy, vz, afx, afy, afz, yaw, yaw_rate, type_mask, target_system, target_component, coordinate_frame
- `PositionTargetGlobalInt` (ID 87, 51 bytes): same fields minus target system/component
- `HomePosition` (ID 242, 52 bytes): latitude, longitude, altitude, x, y, z, q (4 floats), approach_x/y/z, time_usec
- `ExtendedSysState` (ID 245, 2 bytes): vtol_state, landed_state

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ -v n`
Expected: All tests PASS.

- [ ] **Step 4: Commit**

```bash
git add ResQ.Mavlink/Messages/ tests/ResQ.Mavlink.Tests/Messages/
git commit -m "feat(mavlink): implement all Phase 1 message types (~25 common messages)"
```

---

### Task 8: Implement MessageRegistry

**Files:**
- Create: `ResQ.Mavlink/Messages/MessageRegistry.cs`
- Create: `tests/ResQ.Mavlink.Tests/Messages/MessageRegistryTests.cs`

- [ ] **Step 1: Write registry tests**

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

using FluentAssertions;
using ResQ.Mavlink.Messages;

namespace ResQ.Mavlink.Tests.Messages;

public class MessageRegistryTests
{
    [Fact]
    public void TryDeserialize_Heartbeat_Succeeds()
    {
        var hb = new Heartbeat { Type = Enums.MavType.Quadrotor, MavlinkVersion = 3 };
        var buf = new byte[Heartbeat.PayloadSize];
        hb.Serialize(buf);

        var result = MessageRegistry.TryDeserialize(0, buf, out var message);
        result.Should().BeTrue();
        message.Should().BeOfType<Heartbeat>();
        ((Heartbeat)message!).Type.Should().Be(Enums.MavType.Quadrotor);
    }

    [Fact]
    public void TryDeserialize_UnknownMessageId_ReturnsFalse()
    {
        var result = MessageRegistry.TryDeserialize(99999, ReadOnlySpan<byte>.Empty, out _);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRegistered_AllPhase1Messages_ReturnsTrue()
    {
        uint[] phase1Ids = [0, 1, 11, 20, 22, 23, 24, 30, 33, 40, 42, 44, 47, 70, 73, 74, 76, 77, 86, 87, 242, 245, 253];
        foreach (var id in phase1Ids)
        {
            MessageRegistry.IsRegistered(id).Should().BeTrue($"Message ID {id} should be registered");
        }
    }
}
```

- [ ] **Step 2: Run tests — expect fail**

- [ ] **Step 3: Implement MessageRegistry**

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

namespace ResQ.Mavlink.Messages;

/// <summary>
/// Registry mapping message IDs to deserialization functions.
/// </summary>
/// <remarks>
/// Uses <c>byte[]</c> in delegate signatures because <c>ReadOnlySpan&lt;byte&gt;</c> cannot
/// be captured as a generic type argument. Callers pass <c>payload.ToArray()</c> or the
/// backing array from <c>ReadOnlyMemory&lt;byte&gt;</c>.
/// </remarks>
public static class MessageRegistry
{
    private static readonly ConcurrentDictionary<uint, Func<byte[], IMavlinkMessage>> Deserializers = new(
        new Dictionary<uint, Func<byte[], IMavlinkMessage>>
        {
            [0] = buf => Heartbeat.Deserialize(buf),
            [1] = buf => SysStatus.Deserialize(buf),
            [11] = buf => SetMode.Deserialize(buf),
            [20] = buf => ParamRequestRead.Deserialize(buf),
            [22] = buf => ParamValue.Deserialize(buf),
            [23] = buf => ParamSet.Deserialize(buf),
            [24] = buf => GpsRawInt.Deserialize(buf),
            [30] = buf => Attitude.Deserialize(buf),
            [33] = buf => GlobalPositionInt.Deserialize(buf),
            [40] = buf => MissionRequest.Deserialize(buf),
            [42] = buf => MissionCurrent.Deserialize(buf),
            [44] = buf => MissionCount.Deserialize(buf),
            [47] = buf => MissionAck.Deserialize(buf),
            [70] = buf => RcChannelsOverride.Deserialize(buf),
            [73] = buf => MissionItemInt.Deserialize(buf),
            [74] = buf => VfrHud.Deserialize(buf),
            [76] = buf => CommandLong.Deserialize(buf),
            [77] = buf => CommandAck.Deserialize(buf),
            [86] = buf => SetPositionTargetGlobalInt.Deserialize(buf),
            [87] = buf => PositionTargetGlobalInt.Deserialize(buf),
            [242] = buf => HomePosition.Deserialize(buf),
            [245] = buf => ExtendedSysState.Deserialize(buf),
            [253] = buf => StatusText.Deserialize(buf),
        });

    /// <summary>
    /// Attempts to deserialize a payload into a typed message.
    /// </summary>
    public static bool TryDeserialize(uint messageId, byte[] payload, out IMavlinkMessage? message)
    {
        if (Deserializers.TryGetValue(messageId, out var factory))
        {
            message = factory(payload);
            return true;
        }
        message = null;
        return false;
    }

    /// <summary>Returns whether a message ID has a registered deserializer.</summary>
    public static bool IsRegistered(uint messageId) => Deserializers.ContainsKey(messageId);

    /// <summary>
    /// Registers a custom deserializer. Thread-safe. Used by dialect extensions.
    /// </summary>
    public static void Register(uint messageId, Func<byte[], IMavlinkMessage> deserializer) =>
        Deserializers[messageId] = deserializer;
}

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ -v n`
Expected: All tests PASS.

- [ ] **Step 5: Commit**

```bash
git add ResQ.Mavlink/Messages/MessageRegistry.cs tests/ResQ.Mavlink.Tests/Messages/MessageRegistryTests.cs
git commit -m "feat(mavlink): implement message registry with all Phase 1 deserializers"
```

---

## Chunk 3: Transport + Connection

### Task 9: Implement IMavlinkTransport and UdpTransport

**Files:**
- Create: `ResQ.Mavlink/Transport/IMavlinkTransport.cs`
- Create: `ResQ.Mavlink/Transport/TransportState.cs`
- Create: `ResQ.Mavlink/Transport/TransportDisconnectedException.cs`
- Create: `ResQ.Mavlink/Transport/UdpTransport.cs`
- Create: `ResQ.Mavlink/Transport/UdpTransportOptions.cs`
- Create: `tests/ResQ.Mavlink.Tests/Transport/UdpTransportTests.cs`

- [ ] **Step 1: Write UDP transport loopback test**

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

using FluentAssertions;
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;

namespace ResQ.Mavlink.Tests.Transport;

public class UdpTransportTests
{
    [Fact]
    public async Task SendAndReceive_Loopback_DeliversPacket()
    {
        // Two transports on different ports, talking to each other
        var optionsA = new UdpTransportOptions { ListenPort = 14580, RemotePort = 14581 };
        var optionsB = new UdpTransportOptions { ListenPort = 14581, RemotePort = 14580 };

        await using var transportA = new UdpTransport(optionsA);
        await using var transportB = new UdpTransport(optionsB);

        // Build a valid heartbeat packet
        byte[] payload = [0x00, 0x00, 0x00, 0x00, 0x02, 0x03, 0x51, 0x04, 0x03];
        var packet = new MavlinkPacket(1, 1, 1, 0, payload, 0, 0, null);

        // Send from A
        await transportA.SendAsync(packet);

        // Receive on B
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        MavlinkPacket? received = null;
        await foreach (var p in transportB.ReceiveAsync(cts.Token))
        {
            received = p;
            break;
        }

        received.Should().NotBeNull();
        received!.MessageId.Should().Be(0u);
        received.SystemId.Should().Be(1);
    }

    [Fact]
    public async Task State_AfterConstruction_IsConnected()
    {
        var options = new UdpTransportOptions { ListenPort = 14582 };
        await using var transport = new UdpTransport(options);
        transport.State.Should().Be(TransportState.Connected);
    }

    [Fact]
    public async Task State_AfterDispose_IsDisposed()
    {
        var options = new UdpTransportOptions { ListenPort = 14583 };
        var transport = new UdpTransport(options);
        await transport.DisposeAsync();
        transport.State.Should().Be(TransportState.Disposed);
    }
}
```

- [ ] **Step 2: Run tests — expect fail**

- [ ] **Step 3: Implement TransportState and exception**

```csharp
namespace ResQ.Mavlink.Transport;

/// <summary>Transport connection state.</summary>
public enum TransportState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Disposed,
}
```

```csharp
namespace ResQ.Mavlink.Transport;

/// <summary>Thrown when sending on a disconnected transport.</summary>
public sealed class TransportDisconnectedException : InvalidOperationException
{
    public TransportDisconnectedException()
        : base("Cannot send: transport is not connected.") { }
}
```

- [ ] **Step 4: Implement IMavlinkTransport**

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

using ResQ.Mavlink.Protocol;

namespace ResQ.Mavlink.Transport;

/// <summary>
/// Pluggable async transport for MAVLink packets.
/// </summary>
public interface IMavlinkTransport : IAsyncDisposable
{
    /// <summary>Sends a packet over this transport.</summary>
    /// <exception cref="TransportDisconnectedException">Thrown if transport is not connected.</exception>
    ValueTask SendAsync(MavlinkPacket packet, CancellationToken ct = default);

    /// <summary>Yields received packets until cancellation or transport closure.</summary>
    IAsyncEnumerable<MavlinkPacket> ReceiveAsync(CancellationToken ct = default);

    /// <summary>Gets the current transport state.</summary>
    TransportState State { get; }

    /// <summary>Yields state changes as they occur.</summary>
    IAsyncEnumerable<TransportState> StateChanges(CancellationToken ct = default);
}
```

- [ ] **Step 5: Implement UdpTransportOptions**

```csharp
namespace ResQ.Mavlink.Transport;

/// <summary>Configuration for UDP MAVLink transport.</summary>
public sealed class UdpTransportOptions
{
    /// <summary>Local port to listen on. Default 14550.</summary>
    public int ListenPort { get; set; } = 14550;

    /// <summary>Remote port to send to. Default 14550.</summary>
    public int RemotePort { get; set; } = 14550;

    /// <summary>Remote host. Default "127.0.0.1".</summary>
    public string RemoteHost { get; set; } = "127.0.0.1";

    /// <summary>Receive buffer size in bytes. Default 65535.</summary>
    public int ReceiveBufferSize { get; set; } = 65535;
}
```

- [ ] **Step 6: Implement UdpTransport**

The implementer should create a `UdpClient`-based transport that:
- Constructor accepts both `IOptions<UdpTransportOptions>` (for DI) and raw `UdpTransportOptions` (for tests). Add `Microsoft.Extensions.Options` to the `.csproj` dependencies.
- Binds to `ListenPort` on construction
- `SendAsync`: serializes packet via `MavlinkCodec.Serialize()`, sends UDP datagram to `RemoteHost:RemotePort`
- `ReceiveAsync`: loops on `UdpClient.ReceiveAsync()`, parses each datagram via `MavlinkCodec.TryParse()`, yields successfully parsed packets
- `State` is `Connected` immediately (UDP is connectionless), `Disposed` after dispose
- `StateChanges` yields `Connected` on start, `Disposed` on dispose
- `DisposeAsync` closes the `UdpClient`

- [ ] **Step 7: Run tests**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ --filter "FullyQualifiedName~UdpTransportTests" -v n`
Expected: All 3 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add ResQ.Mavlink/Transport/ tests/ResQ.Mavlink.Tests/Transport/
git commit -m "feat(mavlink): implement UDP transport with IMavlinkTransport interface"
```

---

### Task 10: Implement MavlinkConnection

**Files:**
- Create: `ResQ.Mavlink/Connection/MavlinkConnection.cs`
- Create: `ResQ.Mavlink/Connection/MavlinkConnectionOptions.cs`
- Create: `ResQ.Mavlink/Connection/AsyncEnumerableExtensions.cs`
- Create: `tests/ResQ.Mavlink.Tests/Connection/MavlinkConnectionTests.cs`
- Create: `tests/ResQ.Mavlink.Tests/Connection/HeartbeatProtocolTests.cs`

- [ ] **Step 1: Write connection tests**

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

using FluentAssertions;
using ResQ.Mavlink.Connection;
using ResQ.Mavlink.Enums;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;

namespace ResQ.Mavlink.Tests.Connection;

public class MavlinkConnectionTests
{
    [Fact]
    public async Task SendCommandAsync_SerializesAndSends()
    {
        var transport = new FakeTransport();
        var options = new MavlinkConnectionOptions { SystemId = 255, ComponentId = 190 };
        await using var connection = new MavlinkConnection(transport, options);

        var cmd = new CommandLong
        {
            TargetSystem = 1,
            TargetComponent = 1,
            Command = MavCmd.ComponentArmDisarm,
            Param1 = 1.0f,
        };

        // Fire-and-forget send (no ack waiting in this test)
        await connection.SendMessageAsync(cmd);

        transport.SentPackets.Should().HaveCount(1);
        transport.SentPackets[0].MessageId.Should().Be(76u);
    }

    [Fact]
    public void Constructor_SetsSystemAndComponentId()
    {
        var transport = new FakeTransport();
        var options = new MavlinkConnectionOptions { SystemId = 42, ComponentId = 99 };
        using var connection = new MavlinkConnection(transport, options);

        connection.SystemId.Should().Be(42);
        connection.ComponentId.Should().Be(99);
    }

    /// <summary>Minimal fake transport for unit testing without UDP.</summary>
    private sealed class FakeTransport : IMavlinkTransport
    {
        public List<MavlinkPacket> SentPackets { get; } = new();
        public TransportState State => TransportState.Connected;

        public ValueTask SendAsync(MavlinkPacket packet, CancellationToken ct = default)
        {
            SentPackets.Add(packet);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<MavlinkPacket> ReceiveAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            // Never yields — tests that need received packets will use a different fake
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
            yield break;
        }

        public async IAsyncEnumerable<TransportState> StateChanges(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
            yield break;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
```

- [ ] **Step 2: Run tests — expect fail**

- [ ] **Step 3: Implement MavlinkConnectionOptions**

```csharp
namespace ResQ.Mavlink.Connection;

/// <summary>Configuration for a MAVLink connection.</summary>
public sealed class MavlinkConnectionOptions
{
    /// <summary>This system's ID (1-255). Default 255 (GCS).</summary>
    public byte SystemId { get; set; } = 255;

    /// <summary>This component's ID (1-255). Default 190 (MAV_COMP_ID_MISSIONPLANNER).</summary>
    public byte ComponentId { get; set; } = 190;

    /// <summary>Heartbeat send interval. Default 1 second.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Command ack timeout. Default 1500ms.</summary>
    public TimeSpan CommandAckTimeout { get; set; } = TimeSpan.FromMilliseconds(1500);

    /// <summary>Command retry count. Default 3.</summary>
    public int CommandRetryCount { get; set; } = 3;
}
```

- [ ] **Step 4: Implement MavlinkConnection**

The implementer should create a connection class that:
- Constructor accepts both `IOptions<MavlinkConnectionOptions>` (for DI) and raw `MavlinkConnectionOptions` (for tests)
- Takes `IMavlinkTransport` and `MavlinkConnectionOptions`
- Exposes `SystemId`, `ComponentId`
- `SendMessageAsync(IMavlinkMessage)`: wraps in `MavlinkPacket` with auto-incrementing sequence number, serializes via codec, sends via transport
- `ReceiveMessagesAsync<T>()`: filters received packets, deserializes via `MessageRegistry`, yields typed messages
- `StartHeartbeat()`: background task sending `Heartbeat` at configured interval
- `SendCommandAsync(CommandLong)`: sends command, waits for matching `CommandAck` with timeout and retry
- Implements `IAsyncDisposable` — cancels heartbeat, disposes transport

- [ ] **Step 5: Implement AsyncEnumerableExtensions**

```csharp
using System.Runtime.CompilerServices;

namespace ResQ.Mavlink.Connection;

/// <summary>Extensions for filtering IAsyncEnumerable of MAVLink packets.</summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>Filters an async sequence to elements of a specific type.</summary>
    public static async IAsyncEnumerable<T> OfType<T>(
        this IAsyncEnumerable<object> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in source.WithCancellation(ct))
        {
            if (item is T typed)
                yield return typed;
        }
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ --filter "FullyQualifiedName~MavlinkConnectionTests" -v n`
Expected: All tests PASS.

- [ ] **Step 7: Run full test suite**

Run: `dotnet test tests/ResQ.Mavlink.Tests/ -v n`
Expected: All tests PASS.

- [ ] **Step 8: Commit**

```bash
git add ResQ.Mavlink/Connection/ tests/ResQ.Mavlink.Tests/Connection/
git commit -m "feat(mavlink): implement MavlinkConnection with heartbeat, command, and param protocols"
```

---

## Chunk 4: SITL Bridge (IFlightBackend + Adapter + ArduPilotSitlBackend)

### Task 11: Scaffold ResQ.Mavlink.Sitl project

**Files:**
- Create: `ResQ.Mavlink.Sitl/ResQ.Mavlink.Sitl.csproj`
- Create: `tests/ResQ.Mavlink.Sitl.Tests/ResQ.Mavlink.Sitl.Tests.csproj`
- Modify: `ResQ.Sdk.sln`

- [ ] **Step 1: Create ResQ.Mavlink.Sitl.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>ResQ.Mavlink.Sitl</RootNamespace>
    <PackageId>ResQ.Mavlink.Sitl</PackageId>
    <Version>0.1.0</Version>
    <Authors>ResQ Software</Authors>
    <Description>ArduPilot SITL bridge — integrates ArduPilot flight controllers with the ResQ simulation engine</Description>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/resq-software/dotnet-sdk</PackageProjectUrl>
    <RepositoryUrl>https://github.com/resq-software/dotnet-sdk</RepositoryUrl>
    <PackageTags>resq;mavlink;sitl;ardupilot;simulation</PackageTags>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ResQ.Mavlink\ResQ.Mavlink.csproj" />
    <ProjectReference Include="..\ResQ.Simulation.Engine\ResQ.Simulation.Engine.csproj" />
  </ItemGroup>

</Project>
```

**Note:** This depends on `ResQ.Simulation.Engine` being merged from its worktree. If not yet merged, the project will not build until that prerequisite is met. The implementer should merge the sim-engine worktree first, or create a stub `IFlightModel` interface in a shared location.

- [ ] **Step 2: Create test project and add to solution**

Follow same pattern as Task 1 step 2. Test project references `ResQ.Mavlink.Sitl`.

```bash
dotnet sln ResQ.Sdk.sln add ResQ.Mavlink.Sitl/ResQ.Mavlink.Sitl.csproj
dotnet sln ResQ.Sdk.sln add tests/ResQ.Mavlink.Sitl.Tests/ResQ.Mavlink.Sitl.Tests.csproj --solution-folder tests
```

- [ ] **Step 3: Commit**

```bash
git add ResQ.Mavlink.Sitl/ tests/ResQ.Mavlink.Sitl.Tests/ ResQ.Sdk.sln
git commit -m "feat(mavlink-sitl): scaffold ResQ.Mavlink.Sitl project and test project"
```

---

### Task 12: Implement IFlightBackend and FlightModelBackendAdapter

**Files:**
- Create: `ResQ.Mavlink.Sitl/IFlightBackend.cs`
- Create: `ResQ.Mavlink.Sitl/FlightBackendCapabilities.cs`
- Create: `ResQ.Mavlink.Sitl/FlightModelBackendAdapter.cs`
- Create: `tests/ResQ.Mavlink.Sitl.Tests/FlightModelBackendAdapterTests.cs`

- [ ] **Step 1: Write adapter tests**

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

using System.Numerics;
using FluentAssertions;
using ResQ.Mavlink.Sitl;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl.Tests;

public class FlightModelBackendAdapterTests
{
    [Fact]
    public async Task StepAsync_DelegatesToFlightModelStep_WithWind()
    {
        var model = new KinematicFlightModel(Vector3.Zero);
        var adapter = new FlightModelBackendAdapter(model);

        var wind = new Vector3(1.0f, 0, 0);
        var state = await adapter.StepAsync(1.0 / 60.0, wind);

        // After one step with wind, the kinematic model should have updated state
        state.Should().NotBe(default(DronePhysicsState));
    }

    [Fact]
    public async Task SendCommandAsync_DelegatesToApplyCommand()
    {
        var model = new KinematicFlightModel(new Vector3(0, 50, 0));
        var adapter = new FlightModelBackendAdapter(model);

        await adapter.SendCommandAsync(
            FlightCommand.GoTo(new Vector3(100, 50, 0)));

        // Step forward and verify the drone is moving toward target
        var state1 = await adapter.StepAsync(1.0, Vector3.Zero);
        state1.Velocity.Length().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task InitializeAsync_AcceptsDroneConfig()
    {
        var model = new KinematicFlightModel(Vector3.Zero);
        var adapter = new FlightModelBackendAdapter(model);
        var config = new DroneConfig("drone-1", Vector3.Zero);

        // Should not throw — adapter ignores config (model already constructed)
        await adapter.InitializeAsync(config);
    }

    [Fact]
    public void Capabilities_DoesNotIncludeGps()
    {
        var model = new KinematicFlightModel(Vector3.Zero);
        var adapter = new FlightModelBackendAdapter(model);

        adapter.Capabilities.HasFlag(FlightBackendCapabilities.SupportsGps).Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests — expect fail**

- [ ] **Step 3: Implement FlightBackendCapabilities**

```csharp
namespace ResQ.Mavlink.Sitl;

/// <summary>Flags describing what a flight backend supports.</summary>
[Flags]
public enum FlightBackendCapabilities
{
    None = 0,
    SupportsGps = 1 << 0,
    SupportsFaultInjection = 1 << 1,
    SupportsMultipleVehicleTypes = 1 << 2,
    SupportsParameterProtocol = 1 << 3,
    SupportsMissionProtocol = 1 << 4,
}
```

- [ ] **Step 4: Implement IFlightBackend**

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

using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl;

/// <summary>
/// Async flight backend abstraction. Coexists with the synchronous <see cref="IFlightModel"/>
/// via <see cref="FlightModelBackendAdapter"/>.
/// </summary>
public interface IFlightBackend : IAsyncDisposable
{
    /// <summary>Initializes the backend with drone configuration (e.g., spawns SITL process).</summary>
    /// <param name="config">Drone configuration (vehicle type, mass, start position).</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask InitializeAsync(DroneConfig config, CancellationToken ct = default);

    /// <summary>
    /// Advances one simulation tick and returns the new physics state.
    /// For adapter-wrapped <see cref="IFlightModel"/> implementations, this calls
    /// <see cref="IFlightModel.Step"/> synchronously and returns the result.
    /// </summary>
    /// <param name="dt">Timestep in seconds.</param>
    /// <param name="wind">Wind vector from the weather system at the drone's position.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask<DronePhysicsState> StepAsync(double dt, Vector3 wind, CancellationToken ct = default);

    /// <summary>Sends a flight command to the backend.</summary>
    ValueTask SendCommandAsync(FlightCommand command, CancellationToken ct = default);

    /// <summary>Gets the capabilities of this backend.</summary>
    FlightBackendCapabilities Capabilities { get; }
}

/// <summary>
/// Configuration for a drone instance within the simulation.
/// </summary>
/// <param name="Id">Unique drone identifier.</param>
/// <param name="StartPosition">World-space launch position.</param>
/// <param name="VehicleType">Vehicle type string (e.g., "ArduCopter", "ArduPlane").</param>
/// <param name="Mass">Drone mass in kilograms. Default 2.5.</param>
public readonly record struct DroneConfig(
    string Id,
    Vector3 StartPosition,
    string VehicleType = "ArduCopter",
    double Mass = 2.5);
```

- [ ] **Step 5: Implement FlightModelBackendAdapter**

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

using System.Numerics;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl;

/// <summary>
/// Wraps a synchronous <see cref="IFlightModel"/> into the async <see cref="IFlightBackend"/>
/// interface, enabling existing flight models to work alongside ArduPilot SITL backends.
/// </summary>
public sealed class FlightModelBackendAdapter : IFlightBackend
{
    private readonly IFlightModel _model;

    /// <summary>Creates a new adapter wrapping <paramref name="model"/>.</summary>
    public FlightModelBackendAdapter(IFlightModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <inheritdoc/>
    public FlightBackendCapabilities Capabilities => FlightBackendCapabilities.None;

    /// <inheritdoc/>
    public ValueTask InitializeAsync(DroneConfig config, CancellationToken ct = default) =>
        ValueTask.CompletedTask;

    /// <inheritdoc/>
    public ValueTask<DronePhysicsState> StepAsync(double dt, Vector3 wind, CancellationToken ct = default)
    {
        _model.Step(dt, wind);
        return ValueTask.FromResult(_model.State);
    }

    /// <inheritdoc/>
    public ValueTask SendCommandAsync(FlightCommand command, CancellationToken ct = default)
    {
        _model.ApplyCommand(command);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/ResQ.Mavlink.Sitl.Tests/ -v n`
Expected: All 4 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add ResQ.Mavlink.Sitl/IFlightBackend.cs ResQ.Mavlink.Sitl/FlightBackendCapabilities.cs ResQ.Mavlink.Sitl/FlightModelBackendAdapter.cs tests/ResQ.Mavlink.Sitl.Tests/
git commit -m "feat(mavlink-sitl): implement IFlightBackend and FlightModelBackendAdapter"
```

---

### Task 13: Implement SitlTelemetryMapper

**Files:**
- Create: `ResQ.Mavlink.Sitl/SitlTelemetryMapper.cs`
- Create: `tests/ResQ.Mavlink.Sitl.Tests/SitlTelemetryMapperTests.cs`

- [ ] **Step 1: Write mapper tests**

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

using System.Numerics;
using FluentAssertions;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Sitl;

namespace ResQ.Mavlink.Sitl.Tests;

public class SitlTelemetryMapperTests
{
    [Fact]
    public void MapPosition_ConvertsFromDegE7ToMetres()
    {
        var position = new GlobalPositionInt
        {
            Lat = 473977418,     // 47.3977418°
            Lon = 85255792,      // 8.5255792°
            Alt = 408000,        // 408m
            RelativeAlt = 50000, // 50m
        };

        var state = SitlTelemetryMapper.MapToPhysicsState(position, null);

        // Position should be non-zero (exact conversion depends on reference frame)
        state.Position.Should().NotBe(Vector3.Zero);
        state.BatteryPercent.Should().Be(100.0); // no SysStatus provided
    }

    [Fact]
    public void MapPosition_WithAttitude_SetsOrientation()
    {
        var position = new GlobalPositionInt
        {
            Lat = 473977418,
            Lon = 85255792,
            Alt = 408000,
            RelativeAlt = 50000,
        };

        var attitude = new Attitude
        {
            Roll = 0.1f,
            Pitch = 0.2f,
            Yaw = 1.57f, // ~90°
            Rollspeed = 0.01f,
            Pitchspeed = 0.02f,
            Yawspeed = 0.03f,
        };

        var state = SitlTelemetryMapper.MapToPhysicsState(position, attitude);

        state.Orientation.Should().NotBe(Quaternion.Identity);
        state.AngularVelocity.Should().NotBe(Vector3.Zero);
    }
}
```

- [ ] **Step 2: Run tests — expect fail**

- [ ] **Step 3: Implement SitlTelemetryMapper**

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

using System.Numerics;
using ResQ.Mavlink.Messages;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl;

/// <summary>
/// Maps MAVLink telemetry messages to <see cref="DronePhysicsState"/>.
/// </summary>
public static class SitlTelemetryMapper
{
    /// <summary>
    /// Converts MAVLink position and attitude data into a <see cref="DronePhysicsState"/>.
    /// </summary>
    /// <param name="position">GLOBAL_POSITION_INT message.</param>
    /// <param name="attitude">ATTITUDE message, or null if not yet received.</param>
    /// <param name="batteryPercent">Battery percentage from SYS_STATUS, or 100 if unavailable.</param>
    /// <returns>The mapped physics state.</returns>
    public static DronePhysicsState MapToPhysicsState(
        GlobalPositionInt position,
        Attitude? attitude,
        double batteryPercent = 100.0)
    {
        // Convert lat/lon from degE7 to a local ENU frame (simplified: metres from origin)
        // For SITL, we use a simple equirectangular approximation relative to home
        var lat = position.Lat / 1e7;
        var lon = position.Lon / 1e7;

        // Approximate conversion: 1 degree lat ≈ 111320m, 1 degree lon ≈ 111320 * cos(lat)
        var x = (float)(lon * 111320.0 * Math.Cos(lat * Math.PI / 180.0));
        var y = position.RelativeAlt / 1000.0f; // mm to m, use as "up"
        var z = (float)(lat * 111320.0);

        var pos = new Vector3(x, y, z);

        // Velocity: cm/s to m/s
        var vel = new Vector3(
            position.Vx / 100.0f,
            -position.Vz / 100.0f, // MAVLink Z is down, sim Y is up
            position.Vy / 100.0f);

        // Orientation from attitude
        var orientation = Quaternion.Identity;
        var angularVelocity = Vector3.Zero;

        if (attitude is { } att)
        {
            orientation = Quaternion.CreateFromYawPitchRoll(att.Yaw, att.Pitch, att.Roll);
            angularVelocity = new Vector3(att.Rollspeed, att.Yawspeed, att.Pitchspeed);
        }

        return new DronePhysicsState(pos, vel, orientation, angularVelocity, batteryPercent);
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/ResQ.Mavlink.Sitl.Tests/ --filter "FullyQualifiedName~SitlTelemetryMapperTests" -v n`
Expected: All tests PASS.

- [ ] **Step 5: Commit**

```bash
git add ResQ.Mavlink.Sitl/SitlTelemetryMapper.cs tests/ResQ.Mavlink.Sitl.Tests/SitlTelemetryMapperTests.cs
git commit -m "feat(mavlink-sitl): implement MAVLink telemetry → DronePhysicsState mapper"
```

---

### Task 14: Implement JsonPhysicsBridge

**Files:**
- Create: `ResQ.Mavlink.Sitl/JsonPhysicsBridge.cs`
- Create: `tests/ResQ.Mavlink.Sitl.Tests/JsonPhysicsBridgeTests.cs`

This sends sensor data to ArduPilot's JSON physics interface at 400 Hz. ArduPilot's `--model json` mode listens on a UDP socket for JSON-encoded sensor state.

- [ ] **Step 1: Write bridge tests**

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

using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using ResQ.Mavlink.Sitl;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl.Tests;

public class JsonPhysicsBridgeTests
{
    [Fact]
    public void BuildSensorJson_ProducesValidJson()
    {
        var state = DronePhysicsState.AtPosition(new Vector3(10, 50, 20));
        var json = JsonPhysicsBridge.BuildSensorJson(state, 1000);

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("timestamp").GetInt64().Should().Be(1000);
        root.GetProperty("imu").GetProperty("gyro").GetArrayLength().Should().Be(3);
        root.GetProperty("imu").GetProperty("accel_body").GetArrayLength().Should().Be(3);
        root.GetProperty("position").GetArrayLength().Should().Be(3);
        root.GetProperty("velocity").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public void BuildSensorJson_AccelIncludesGravity()
    {
        var state = DronePhysicsState.AtPosition(Vector3.Zero);
        var json = JsonPhysicsBridge.BuildSensorJson(state, 0);

        var doc = JsonDocument.Parse(json);
        var accel = doc.RootElement.GetProperty("imu").GetProperty("accel_body");

        // At rest, Z accel should be approximately -9.81 (gravity in body frame, pointing down)
        var az = accel[2].GetDouble();
        az.Should().BeApproximately(-9.81, 0.1);
    }
}
```

- [ ] **Step 2: Run tests — expect fail**

- [ ] **Step 3: Implement JsonPhysicsBridge**

The implementer should create a class that:
- `BuildSensorJson(DronePhysicsState, long timestampMicros)`: produces the JSON string matching ArduPilot's JSON physics interface format (see ArduPilot source: `libraries/SITL/SIM_JSON.cpp`)
- Fields: timestamp, imu (gyro[3], accel_body[3]), position[3], velocity[3], quaternion[4], airspeed
- At rest, accel_body should include gravity (-9.81 on Z axis in body frame)
- `SendAsync(DronePhysicsState, long, UdpClient, IPEndPoint)`: serializes and sends via UDP

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/ResQ.Mavlink.Sitl.Tests/ --filter "FullyQualifiedName~JsonPhysicsBridgeTests" -v n`
Expected: All tests PASS.

- [ ] **Step 5: Commit**

```bash
git add ResQ.Mavlink.Sitl/JsonPhysicsBridge.cs tests/ResQ.Mavlink.Sitl.Tests/JsonPhysicsBridgeTests.cs
git commit -m "feat(mavlink-sitl): implement JSON physics bridge for ArduPilot SITL"
```

---

### Task 15: Implement SitlProcessManager

**Files:**
- Create: `ResQ.Mavlink.Sitl/SitlProcessManager.cs`
- Create: `ResQ.Mavlink.Sitl/SitlProcessManagerOptions.cs`

This manages spawning and killing ArduPilot SITL processes. No integration tests here — those require SITL binaries installed.

- [ ] **Step 1: Implement SitlProcessManagerOptions**

```csharp
namespace ResQ.Mavlink.Sitl;

/// <summary>Configuration for the SITL process pool.</summary>
public sealed class SitlProcessManagerOptions
{
    /// <summary>Path to ArduPilot SITL binary (e.g., "arducopter"). Default assumes it's on PATH.</summary>
    public string SitlBinaryPath { get; set; } = "arducopter";

    /// <summary>Base UDP port for MAVLink. Each instance gets base + (2 * instanceIndex). Default 5760.</summary>
    public int BasePort { get; set; } = 5760;

    /// <summary>Base port for JSON physics socket. Each instance gets base + instanceIndex. Default 9002.</summary>
    public int BaseJsonPort { get; set; } = 9002;

    /// <summary>Maximum concurrent SITL instances. Default 20.</summary>
    public int MaxInstances { get; set; } = 20;

    /// <summary>Home location for SITL as "lat,lon,alt,heading". Default Zurich.</summary>
    public string HomeLocation { get; set; } = "47.3977418,8.5255792,408,0";

    /// <summary>Vehicle type. Default "json" (JSON physics input).</summary>
    public string Model { get; set; } = "json";
}
```

- [ ] **Step 2: Implement SitlProcessManager**

The implementer should create a class that:
- Implements `IAsyncDisposable`
- `SpawnAsync(int instanceIndex)`: spawns an `arducopter` process with args: `--model json --home {Home} --instance {index} --base-port {basePort + 2*index}`
- Tracks spawned `Process` objects by instance index
- `KillAsync(int instanceIndex)`: kills process gracefully (SIGTERM), force kill after 5s
- `DisposeAsync()`: kills all managed processes
- `GetMavlinkPort(int instanceIndex)`: returns `BasePort + 2 * instanceIndex`
- `GetJsonPort(int instanceIndex)`: returns `BaseJsonPort + instanceIndex`
- Thread-safe via `SemaphoreSlim` for max instances

- [ ] **Step 3: Verify build**

Run: `dotnet build ResQ.Mavlink.Sitl/ResQ.Mavlink.Sitl.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add ResQ.Mavlink.Sitl/SitlProcessManager.cs ResQ.Mavlink.Sitl/SitlProcessManagerOptions.cs
git commit -m "feat(mavlink-sitl): implement SITL process manager with port allocation and pooling"
```

---

### Task 16: Implement ArduPilotSitlBackend

**Files:**
- Create: `ResQ.Mavlink.Sitl/ArduPilotSitlBackend.cs`
- Create: `ResQ.Mavlink.Sitl/SitlBackendOptions.cs`

- [ ] **Step 1: Implement SitlBackendOptions**

```csharp
namespace ResQ.Mavlink.Sitl;

/// <summary>Configuration for an individual ArduPilot SITL backend.</summary>
public sealed class SitlBackendOptions
{
    /// <summary>SITL instance index (determines port allocation). Default 0.</summary>
    public int InstanceIndex { get; set; }

    /// <summary>Physics feed rate in Hz. Default 400.</summary>
    public int PhysicsRateHz { get; set; } = 400;

    /// <summary>Vehicle type string. Default "ArduCopter".</summary>
    public string VehicleType { get; set; } = "ArduCopter";
}
```

- [ ] **Step 2: Implement ArduPilotSitlBackend**

The implementer should create a class that:
- Implements `IFlightBackend`
- `InitializeAsync`: uses `SitlProcessManager.SpawnAsync()` to start SITL, creates `UdpTransport` + `MavlinkConnection` to the SITL MAVLink port, starts JSON physics bridge
- `StepAsync(dt)`: sends latest physics state via `JsonPhysicsBridge`, reads latest `GlobalPositionInt`+`Attitude` from `MavlinkConnection`, maps via `SitlTelemetryMapper`, returns `DronePhysicsState`
- `SendCommandAsync`: translates `FlightCommand` → MAVLink `CommandLong` or `SetPositionTargetGlobalInt`, sends via connection
  - `FlightCommand.Hover` → `SetPositionTargetGlobalInt` with current position + type_mask for position hold
  - `FlightCommand.GoTo(target)` → `SetPositionTargetGlobalInt` with target position
  - `FlightCommand.RTL` → `CommandLong` with `MAV_CMD_NAV_RETURN_TO_LAUNCH`
  - `FlightCommand.Land` → `CommandLong` with `MAV_CMD_NAV_LAND`
- `Capabilities`: returns `SupportsGps | SupportsFaultInjection | SupportsMultipleVehicleTypes | SupportsParameterProtocol | SupportsMissionProtocol`
- `DisposeAsync`: kills SITL process, disposes connection and transport

- [ ] **Step 3: Verify build**

Run: `dotnet build ResQ.Mavlink.Sitl/ResQ.Mavlink.Sitl.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add ResQ.Mavlink.Sitl/ArduPilotSitlBackend.cs ResQ.Mavlink.Sitl/SitlBackendOptions.cs
git commit -m "feat(mavlink-sitl): implement ArduPilotSitlBackend — full SITL flight backend"
```

---

### Task 17: Create SITL integration test (guarded)

**Files:**
- Create: `tests/ResQ.Mavlink.Sitl.Tests/Integration/SitlIntegrationTests.cs`

- [ ] **Step 1: Write guarded integration test**

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

using System.Numerics;
using FluentAssertions;
using ResQ.Mavlink.Sitl;
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl.Tests.Integration;

[Trait("Category", "Integration")]
public class SitlIntegrationTests
{
    private static bool IsSitlAvailable()
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "arducopter",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch { return false; }
    }

    [Fact]
    public async Task SpawnAndConnect_ReceivesHeartbeat()
    {
        if (!IsSitlAvailable())
        {
            // Skip — SITL not installed
            return;
        }

        var processManagerOptions = new SitlProcessManagerOptions();
        await using var processManager = new SitlProcessManager(processManagerOptions);
        var backendOptions = new SitlBackendOptions { InstanceIndex = 0 };

        await using var backend = new ArduPilotSitlBackend(processManager, backendOptions);
        await backend.InitializeAsync();

        // Step a few times — should get valid telemetry
        DronePhysicsState? lastState = null;
        for (var i = 0; i < 100; i++)
        {
            lastState = await backend.StepAsync(1.0 / 400.0, Vector3.Zero);
            await Task.Delay(2); // ~400 Hz
        }

        lastState.Should().NotBeNull();
        lastState!.Value.BatteryPercent.Should().BeGreaterThan(0);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add tests/ResQ.Mavlink.Sitl.Tests/Integration/
git commit -m "test(mavlink-sitl): add guarded SITL integration test"
```

---

### Task 18: Run full test suite and verify

- [ ] **Step 1: Run all non-integration tests**

Run: `dotnet test -c Release --filter "Category!=Integration" -v n`
Expected: All tests PASS. No build warnings related to MAVLink projects.

- [ ] **Step 2: Verify build with all projects**

Run: `dotnet build -c Release`
Expected: Build succeeded for entire solution.

- [ ] **Step 3: Final commit if any cleanup needed**

```bash
git add -A
git commit -m "chore(mavlink): Phase 1 cleanup and verification"
```
