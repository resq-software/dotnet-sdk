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
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;
using Xunit;

namespace ResQ.Mavlink.Dialect.Tests;

/// <summary>
/// End-to-end tests: create a MavlinkPacket with a dialect message, serialize to wire bytes,
/// parse back, and verify all fields are preserved through the full MAVLink framing pipeline.
/// </summary>
public sealed class ResqDialectEndToEndTests
{
    static ResqDialectEndToEndTests() => ResqDialectRegistry.Register();

    [Fact]
    public void EndToEnd_ResqDetection_SerializeParsePreservesFields()
    {
        var detection = new ResqDetection
        {
            TimestampMs = 1_711_400_000_123UL,
            LatE7 = 376_874_200,
            LonE7 = -1_222_313_100,
            AltMm = 100_000,
            BboxX = 100,
            BboxY = 50,
            BboxW = 200,
            BboxH = 150,
            DetectionType = ResqDetectionType.Fire,
            Confidence = 95,
        };

        var wireBytes = SerializeDialectMessage(detection, 60000, sequenceNumber: 1);
        var ok = MavlinkCodec.TryParse(wireBytes, out var packet);

        ok.Should().BeTrue();
        packet.Should().NotBeNull();
        packet!.MessageId.Should().Be(60000u);

        var msgOk = MessageRegistry.TryDeserialize(packet.MessageId, packet.Payload.Span, out var msg);
        msgOk.Should().BeTrue();

        var parsed = (ResqDetection)msg!;
        parsed.TimestampMs.Should().Be(detection.TimestampMs);
        parsed.LatE7.Should().Be(detection.LatE7);
        parsed.LonE7.Should().Be(detection.LonE7);
        parsed.AltMm.Should().Be(detection.AltMm);
        parsed.BboxX.Should().Be(detection.BboxX);
        parsed.BboxY.Should().Be(detection.BboxY);
        parsed.BboxW.Should().Be(detection.BboxW);
        parsed.BboxH.Should().Be(detection.BboxH);
        parsed.DetectionType.Should().Be(detection.DetectionType);
        parsed.Confidence.Should().Be(detection.Confidence);
    }

    [Fact]
    public void EndToEnd_ResqEmergencyBeacon_TtlPreservedThroughWire()
    {
        var beacon = new ResqEmergencyBeacon
        {
            TimestampMs = 1_711_700_000_000UL,
            BeaconId = 77u,
            LatE7 = 376_500_000,
            LonE7 = -1_222_000_000,
            AltMm = 0,
            BeaconType = ResqBeaconType.MedicalEmergency,
            Urgency = ResqUrgencyLevel.LifeThreatening,
            Ttl = 4,
        };

        var wireBytes = SerializeDialectMessage(beacon, 60007, sequenceNumber: 2);
        var ok = MavlinkCodec.TryParse(wireBytes, out var packet);

        ok.Should().BeTrue();
        var msgOk = MessageRegistry.TryDeserialize(packet!.MessageId, packet.Payload.Span, out var msg);
        msgOk.Should().BeTrue();

        var parsed = (ResqEmergencyBeacon)msg!;
        parsed.Ttl.Should().Be(4);
        parsed.BeaconType.Should().Be(ResqBeaconType.MedicalEmergency);
    }

    [Fact]
    public void EndToEnd_ResqHazardZone_SerializeParsePreservesFields()
    {
        var zone = new ResqHazardZone
        {
            TimestampMs = 1_711_500_000_000UL,
            ZoneId = 12u,
            CenterLatE7 = 376_000_000,
            CenterLonE7 = -1_221_000_000,
            RadiusMetres = 500u,
            ProgressionSpeed = 1.5f,
            ProgressionHeading = 0.78f,
            HazardType = ResqHazardType.Flood,
            Severity = ResqHazardSeverity.High,
        };

        var wireBytes = SerializeDialectMessage(zone, 60004, sequenceNumber: 3);
        var ok = MavlinkCodec.TryParse(wireBytes, out var packet);

        ok.Should().BeTrue();
        var msgOk = MessageRegistry.TryDeserialize(packet!.MessageId, packet.Payload.Span, out var msg);
        msgOk.Should().BeTrue();

        var parsed = (ResqHazardZone)msg!;
        parsed.ZoneId.Should().Be(zone.ZoneId);
        parsed.RadiusMetres.Should().Be(zone.RadiusMetres);
        parsed.HazardType.Should().Be(zone.HazardType);
        parsed.ProgressionSpeed.Should().BeApproximately(zone.ProgressionSpeed, 1e-5f);
    }

    [Fact]
    public void EndToEnd_ResqDroneCapability_SensorFlagsPreserved()
    {
        var cap = new ResqDroneCapability
        {
            SensorFlags = 0x3F,
            MaxFlightTimeMin = 30,
            MaxSpeedCms = 1500,
            MaxPayloadGrams = 1000,
            CurrentPayloadGrams = 250,
            SystemId = 9,
            DialectVersion = 1,
        };

        var wireBytes = SerializeDialectMessage(cap, 60006, sequenceNumber: 4);
        var ok = MavlinkCodec.TryParse(wireBytes, out var packet);

        ok.Should().BeTrue();
        var msgOk = MessageRegistry.TryDeserialize(packet!.MessageId, packet.Payload.Span, out var msg);
        msgOk.Should().BeTrue();

        var parsed = (ResqDroneCapability)msg!;
        parsed.SensorFlags.Should().Be(0x3F);
        parsed.DialectVersion.Should().Be(1);
    }

    // ── helper ──────────────────────────────────────────────────────────────

    private static byte[] SerializeDialectMessage(IMavlinkMessage message, uint messageId, byte sequenceNumber)
    {
        var payloadSize = GetPayloadSize(message);
        var payload = new byte[payloadSize];
        message.Serialize(payload);

        var packet = new MavlinkPacket(
            sequenceNumber: sequenceNumber,
            systemId: 1,
            componentId: 1,
            messageId: messageId,
            payload: payload,
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

        return MavlinkCodec.Serialize(packet);
    }

    private static int GetPayloadSize(IMavlinkMessage message) => message switch
    {
        ResqDetection => ResqDetection.PayloadSize,
        ResqDetectionAck => ResqDetectionAck.PayloadSize,
        ResqSwarmTask => ResqSwarmTask.PayloadSize,
        ResqSwarmTaskAck => ResqSwarmTaskAck.PayloadSize,
        ResqHazardZone => ResqHazardZone.PayloadSize,
        ResqMeshTopology => ResqMeshTopology.PayloadSize,
        ResqDroneCapability => ResqDroneCapability.PayloadSize,
        ResqEmergencyBeacon => ResqEmergencyBeacon.PayloadSize,
        _ => throw new ArgumentException($"Unknown dialect message type: {message.GetType().Name}"),
    };
}
