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
using ResQ.Mavlink.Dialect.Enums;
using ResQ.Mavlink.Dialect.Messages;
using Xunit;

namespace ResQ.Mavlink.Dialect.Tests;

/// <summary>
/// Round-trip serialize/deserialize tests for all 8 ResQ dialect messages.
/// </summary>
public sealed class ResqDialectRoundTripTests
{
    // ── RESQ_DETECTION ──────────────────────────────────────────────────────

    [Fact]
    public void ResqDetection_RoundTrip_AllFields()
    {
        var original = new ResqDetection
        {
            TimestampMs = 1_711_400_000_000UL,
            LatE7 = 376_874_200,
            LonE7 = -1_222_313_100,
            AltMm = 150_000,
            BboxX = 320,
            BboxY = 240,
            BboxW = 64,
            BboxH = 128,
            DetectionType = ResqDetectionType.Person,
            Confidence = 87,
        };

        var buf = new byte[ResqDetection.PayloadSize];
        original.Serialize(buf);
        var deserialized = ResqDetection.Deserialize(buf);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void ResqDetection_AllDetectionTypes_PreservedInRoundTrip()
    {
        foreach (ResqDetectionType type in Enum.GetValues<ResqDetectionType>())
        {
            var msg = new ResqDetection { DetectionType = type };
            var buf = new byte[ResqDetection.PayloadSize];
            msg.Serialize(buf);
            ResqDetection.Deserialize(buf).DetectionType.Should().Be(type);
        }
    }

    [Fact]
    public void ResqDetection_Confidence100_PreservedInRoundTrip()
    {
        var msg = new ResqDetection { Confidence = 100 };
        var buf = new byte[ResqDetection.PayloadSize];
        msg.Serialize(buf);
        ResqDetection.Deserialize(buf).Confidence.Should().Be(100);
    }

    // ── RESQ_DETECTION_ACK ──────────────────────────────────────────────────

    [Fact]
    public void ResqDetectionAck_RoundTrip_AllFields()
    {
        var original = new ResqDetectionAck
        {
            OriginalTimestampMs = 1_711_400_000_000UL,
            LatE7 = 376_874_200,
            LonE7 = -1_222_313_100,
            AckType = (ResqDetectionAckType)2, // Investigating
            AckerSystemId = 42,
        };

        var buf = new byte[ResqDetectionAck.PayloadSize];
        original.Serialize(buf);
        var deserialized = ResqDetectionAck.Deserialize(buf);

        deserialized.Should().Be(original);
    }

    // ── RESQ_SWARM_TASK ─────────────────────────────────────────────────────

    [Fact]
    public void ResqSwarmTask_RoundTrip_AllFields()
    {
        var original = new ResqSwarmTask
        {
            TaskId = 0xDEAD_BEEFu,
            AreaLat1E7 = 376_000_000,
            AreaLon1E7 = -1_220_000_000,
            AreaLat2E7 = 377_000_000,
            AreaLon2E7 = -1_219_000_000,
            AltMinMm = 50_000,
            AltMaxMm = 120_000,
            TimeoutSec = 600,
            TargetDroneId = 5,
            TaskType = ResqTaskType.Search,
            Priority = (ResqTaskPriority)2, // High
            SearchPattern = (ResqSearchPattern)1, // Spiral
        };

        var buf = new byte[ResqSwarmTask.PayloadSize];
        original.Serialize(buf);
        var deserialized = ResqSwarmTask.Deserialize(buf);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void ResqSwarmTask_AllTaskTypes_PreservedInRoundTrip()
    {
        foreach (ResqTaskType type in Enum.GetValues<ResqTaskType>())
        {
            var msg = new ResqSwarmTask { TaskType = type };
            var buf = new byte[ResqSwarmTask.PayloadSize];
            msg.Serialize(buf);
            ResqSwarmTask.Deserialize(buf).TaskType.Should().Be(type);
        }
    }

    // ── RESQ_SWARM_TASK_ACK ─────────────────────────────────────────────────

    [Fact]
    public void ResqSwarmTaskAck_RoundTrip_AllFields()
    {
        var original = new ResqSwarmTaskAck
        {
            TaskId = 99999u,
            Response = (ResqTaskResponse)2, // Complete
            ProgressPercent = 100,
        };

        var buf = new byte[ResqSwarmTaskAck.PayloadSize];
        original.Serialize(buf);
        var deserialized = ResqSwarmTaskAck.Deserialize(buf);

        deserialized.Should().Be(original);
    }

    // ── RESQ_HAZARD_ZONE ────────────────────────────────────────────────────

    [Fact]
    public void ResqHazardZone_RoundTrip_AllFields()
    {
        var original = new ResqHazardZone
        {
            TimestampMs = 1_711_500_000_000UL,
            ZoneId = 7u,
            CenterLatE7 = 377_000_000,
            CenterLonE7 = -1_221_000_000,
            RadiusMetres = 250u,
            ProgressionSpeed = 2.5f,
            ProgressionHeading = 1.2f,
            HazardType = ResqHazardType.Fire,
            Severity = (ResqHazardSeverity)3, // Extreme
        };

        var buf = new byte[ResqHazardZone.PayloadSize];
        original.Serialize(buf);
        var deserialized = ResqHazardZone.Deserialize(buf);

        deserialized.TimestampMs.Should().Be(original.TimestampMs);
        deserialized.ZoneId.Should().Be(original.ZoneId);
        deserialized.CenterLatE7.Should().Be(original.CenterLatE7);
        deserialized.RadiusMetres.Should().Be(original.RadiusMetres);
        deserialized.ProgressionSpeed.Should().BeApproximately(original.ProgressionSpeed, 1e-5f);
        deserialized.ProgressionHeading.Should().BeApproximately(original.ProgressionHeading, 1e-5f);
        deserialized.HazardType.Should().Be(original.HazardType);
        deserialized.Severity.Should().Be(original.Severity);
    }

    [Fact]
    public void ResqHazardZone_AllHazardTypes_PreservedInRoundTrip()
    {
        foreach (ResqHazardType type in Enum.GetValues<ResqHazardType>())
        {
            var msg = new ResqHazardZone { HazardType = type };
            var buf = new byte[ResqHazardZone.PayloadSize];
            msg.Serialize(buf);
            ResqHazardZone.Deserialize(buf).HazardType.Should().Be(type);
        }
    }

    // ── RESQ_MESH_TOPOLOGY ──────────────────────────────────────────────────

    [Fact]
    public void ResqMeshTopology_RoundTrip_AllFields()
    {
        var original = new ResqMeshTopology
        {
            TimestampMs = 1_711_600_000_000UL,
            ReporterSystemId = 3,
            NeighborCount = 3,
            Neighbor1Id = 1,
            Neighbor1Rssi = 210,
            Neighbor2Id = 2,
            Neighbor2Rssi = 185,
            Neighbor3Id = 4,
            Neighbor3Rssi = 170,
            Neighbor4Id = 0,
            Neighbor4Rssi = 0,
            Neighbor5Id = 0,
            Neighbor5Rssi = 0,
            HasGroundLink = 1,
        };

        var buf = new byte[ResqMeshTopology.PayloadSize];
        original.Serialize(buf);
        var deserialized = ResqMeshTopology.Deserialize(buf);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void ResqMeshTopology_HasGroundLink_PreservedInRoundTrip()
    {
        foreach (byte value in new byte[] { 0, 1 })
        {
            var msg = new ResqMeshTopology { HasGroundLink = value };
            var buf = new byte[ResqMeshTopology.PayloadSize];
            msg.Serialize(buf);
            ResqMeshTopology.Deserialize(buf).HasGroundLink.Should().Be(value);
        }
    }

    // ── RESQ_DRONE_CAPABILITY ───────────────────────────────────────────────

    [Fact]
    public void ResqDroneCapability_RoundTrip_AllFields()
    {
        var original = new ResqDroneCapability
        {
            SensorFlags = 0x3F, // all sensors
            MaxFlightTimeMin = 45,
            MaxSpeedCms = 1800, // 18 m/s
            MaxPayloadGrams = 2000,
            CurrentPayloadGrams = 500,
            SystemId = 7,
            DialectVersion = 1,
        };

        var buf = new byte[ResqDroneCapability.PayloadSize];
        original.Serialize(buf);
        var deserialized = ResqDroneCapability.Deserialize(buf);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void ResqDroneCapability_SensorFlagsBitfield_PreservedInRoundTrip()
    {
        var msg = new ResqDroneCapability { SensorFlags = 0x25 }; // RGB + LiDAR + Spotlight
        var buf = new byte[ResqDroneCapability.PayloadSize];
        msg.Serialize(buf);
        ResqDroneCapability.Deserialize(buf).SensorFlags.Should().Be(0x25);
    }

    // ── RESQ_EMERGENCY_BEACON ───────────────────────────────────────────────

    [Fact]
    public void ResqEmergencyBeacon_RoundTrip_AllFields()
    {
        var original = new ResqEmergencyBeacon
        {
            TimestampMs = 1_711_700_000_000UL,
            BeaconId = 42u,
            LatE7 = 376_500_000,
            LonE7 = -1_222_000_000,
            AltMm = 0,
            BeaconType = ResqBeaconType.PersonInDistress,
            Urgency = (ResqUrgencyLevel)3, // LifeThreatening
            Ttl = 5,
        };

        var buf = new byte[ResqEmergencyBeacon.PayloadSize];
        original.Serialize(buf);
        var deserialized = ResqEmergencyBeacon.Deserialize(buf);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void ResqEmergencyBeacon_TtlField_PreservedInRoundTrip()
    {
        foreach (byte ttl in new byte[] { 0, 1, 7, 255 })
        {
            var msg = new ResqEmergencyBeacon { Ttl = ttl };
            var buf = new byte[ResqEmergencyBeacon.PayloadSize];
            msg.Serialize(buf);
            ResqEmergencyBeacon.Deserialize(buf).Ttl.Should().Be(ttl, $"TTL {ttl} should survive round-trip");
        }
    }

    [Fact]
    public void ResqEmergencyBeacon_AllBeaconTypes_PreservedInRoundTrip()
    {
        foreach (ResqBeaconType type in Enum.GetValues<ResqBeaconType>())
        {
            var msg = new ResqEmergencyBeacon { BeaconType = type };
            var buf = new byte[ResqEmergencyBeacon.PayloadSize];
            msg.Serialize(buf);
            ResqEmergencyBeacon.Deserialize(buf).BeaconType.Should().Be(type);
        }
    }
}
