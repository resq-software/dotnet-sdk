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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Dialect.Messages;
using ResQ.Mavlink.Mesh.Firmware;
using ResQ.Mavlink.Mesh.Tests.Infrastructure;
using ResQ.Mavlink.Protocol;
using Xunit;

namespace ResQ.Mavlink.Mesh.Tests;

/// <summary>
/// Unit tests for <see cref="FirmwareIntegrationService"/>.
/// </summary>
public sealed class FirmwareIntegrationServiceTests
{
    private static FirmwareIntegrationService CreateService(TestTransport transport)
    {
        var opts = Options.Create(new FirmwareIntegrationOptions { OwnSystemId = 1, OwnComponentId = 1 });
        return new FirmwareIntegrationService(transport, opts);
    }

    private static MavlinkPacket MakePacket(uint messageId, byte[] payload, byte systemId = 99)
        => new(
            sequenceNumber: 0,
            systemId: systemId,
            componentId: 1,
            messageId: messageId,
            payload: payload,
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

    [Fact]
    public async Task ReportDetectionAsync_SendsResqDetectionViaMesh()
    {
        var inner = new TestTransport();
        await using var svc = CreateService(inner);

        var detection = new ResqDetection
        {
            TimestampMs = 1000,
            LatE7 = 123456789,
            LonE7 = 987654321,
            AltMm = 50000,
            Confidence = 95,
        };

        await svc.ReportDetectionAsync(detection);

        inner.SentPackets.Should().HaveCount(1);
        inner.SentPackets[0].MessageId.Should().Be(60000u);
        inner.SentPackets[0].SystemId.Should().Be(1);
    }

    [Fact]
    public async Task BroadcastEmergencyAsync_SendsResqEmergencyBeacon()
    {
        var inner = new TestTransport();
        await using var svc = CreateService(inner);

        var beacon = new ResqEmergencyBeacon
        {
            TimestampMs = 2000,
            BeaconId = 42,
            LatE7 = 100000000,
            LonE7 = 200000000,
            AltMm = 10000,
            Urgency = 3,
            Ttl = 7,
        };

        await svc.BroadcastEmergencyAsync(beacon);

        inner.SentPackets.Should().HaveCount(1);
        inner.SentPackets[0].MessageId.Should().Be(60007u);
    }

    [Fact]
    public async Task ReportTaskProgressAsync_SendsResqSwarmTaskAck()
    {
        var inner = new TestTransport();
        await using var svc = CreateService(inner);

        var ack = new ResqSwarmTaskAck { TaskId = 7, Response = 0, ProgressPercent = 50 };
        await svc.ReportTaskProgressAsync(ack);

        inner.SentPackets.Should().HaveCount(1);
        inner.SentPackets[0].MessageId.Should().Be(60003u);
    }

    [Fact]
    public async Task IncomingSwarmTask_DispatchedToTaskAssignmentsStream()
    {
        var inner = new TestTransport();
        await using var svc = CreateService(inner);

        var task = new ResqSwarmTask
        {
            TaskId = 99,
            TargetDroneId = 1,
            TimeoutSec = 300,
            Priority = 2,
        };
        var payload = new byte[ResqSwarmTask.PayloadSize];
        task.Serialize(payload);
        inner.InjectPacket(MakePacket(60002, payload));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var received = new List<ResqSwarmTask>();
        await foreach (var t in svc.TaskAssignments(cts.Token))
        {
            received.Add(t);
            break; // only need the first one
        }

        received.Should().HaveCount(1);
        received[0].TaskId.Should().Be(99u);
    }

    [Fact]
    public async Task GetCurrentTaskAsync_AfterIncomingTask_ReturnsTask()
    {
        var inner = new TestTransport();
        await using var svc = CreateService(inner);

        var swarmTask = new ResqSwarmTask { TaskId = 55, TargetDroneId = 2, TimeoutSec = 60 };
        var payload = new byte[ResqSwarmTask.PayloadSize];
        swarmTask.Serialize(payload);
        inner.InjectPacket(MakePacket(60002, payload));

        // Give the dispatch loop time to process
        await Task.Delay(200);

        var result = await svc.GetCurrentTaskAsync(2);
        result.Should().NotBeNull();
        result!.Value.TaskId.Should().Be(55u);
    }
}
