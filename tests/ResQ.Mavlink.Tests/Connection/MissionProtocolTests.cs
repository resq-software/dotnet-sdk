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
using ResQ.Mavlink.Tests.Infrastructure;
using Xunit;

namespace ResQ.Mavlink.Tests.Connection;

public sealed class MissionProtocolTests
{
    private static MavlinkPacket MakePacket(IMavlinkMessage msg, int payloadSize)
    {
        var payload = new byte[payloadSize];
        msg.Serialize(payload);
        return new MavlinkPacket(0, 1, 1, msg.MessageId, payload, 0, 0, null);
    }

    [Fact]
    public async Task UploadMission_TwoItems_SendsMissionCountAndItems()
    {
        var transport = new ResQ.Mavlink.Tests.Infrastructure.InjectableTransport();
        var protocol = new MissionProtocol(transport, systemId: 255, componentId: 190);

        var items = new[]
        {
            new MissionItemInt { Seq = 0, Command = MavCmd.NavTakeoff, Frame = MavFrame.GlobalRelativeAlt, Z = 10f, TargetSystem = 1 },
            new MissionItemInt { Seq = 1, Command = MavCmd.NavWaypoint, Frame = MavFrame.GlobalRelativeAlt, X = 473977418, Y = 85255792, Z = 50f, TargetSystem = 1 },
        };

        // Simulate vehicle requesting each item and then sending ACK
        var uploadTask = protocol.UploadMissionAsync(items, targetSystem: 1);

        // Give protocol time to send MISSION_COUNT
        await Task.Delay(50);

        // Simulate vehicle sending MISSION_REQUEST for item 0
        transport.InjectPacket(MakePacket(new MissionRequest { Seq = 0, TargetSystem = 255, TargetComponent = 190 }, MissionRequest.PayloadSize));
        await Task.Delay(30);

        // Simulate vehicle sending MISSION_REQUEST for item 1
        transport.InjectPacket(MakePacket(new MissionRequest { Seq = 1, TargetSystem = 255, TargetComponent = 190 }, MissionRequest.PayloadSize));
        await Task.Delay(30);

        // Simulate vehicle sending MISSION_ACK
        transport.InjectPacket(MakePacket(new MissionAck { Type = MavMissionResult.Accepted, TargetSystem = 255, TargetComponent = 190 }, MissionAck.PayloadSize));
        transport.CompleteReceive();

        await uploadTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Protocol sent: MISSION_COUNT + MISSION_ITEM_INT[0] + MISSION_ITEM_INT[1]
        transport.SentPackets.Should().HaveCountGreaterThanOrEqualTo(3);
        transport.SentPackets[0].MessageId.Should().Be(44); // MISSION_COUNT
        transport.SentPackets[1].MessageId.Should().Be(73); // MISSION_ITEM_INT
        transport.SentPackets[2].MessageId.Should().Be(73); // MISSION_ITEM_INT
    }

    [Fact]
    public async Task DownloadMission_TwoItems_ReturnsItems()
    {
        var transport = new ResQ.Mavlink.Tests.Infrastructure.InjectableTransport();
        var protocol = new MissionProtocol(transport, systemId: 255, componentId: 190);

        var downloadTask = protocol.DownloadMissionAsync(targetSystem: 1);

        // Give protocol time to send MISSION_REQUEST_LIST
        await Task.Delay(50);

        // Simulate vehicle sending MISSION_COUNT
        transport.InjectPacket(MakePacket(new MissionCount { Count = 2, TargetSystem = 255, TargetComponent = 190 }, MissionCount.PayloadSize));
        await Task.Delay(30);

        // Simulate vehicle sending item 0 (after protocol sends MISSION_REQUEST for seq=0)
        transport.InjectPacket(MakePacket(new MissionItemInt
        {
            Seq = 0,
            Command = MavCmd.NavTakeoff,
            Frame = MavFrame.GlobalRelativeAlt,
            Z = 10f,
            TargetSystem = 255,
            TargetComponent = 190,
        }, MissionItemInt.PayloadSize));
        await Task.Delay(30);

        // Simulate vehicle sending item 1
        transport.InjectPacket(MakePacket(new MissionItemInt
        {
            Seq = 1,
            Command = MavCmd.NavWaypoint,
            Frame = MavFrame.GlobalRelativeAlt,
            X = 473977418,
            Y = 85255792,
            Z = 50f,
            TargetSystem = 255,
            TargetComponent = 190,
        }, MissionItemInt.PayloadSize));
        transport.CompleteReceive();

        var items = await downloadTask.WaitAsync(TimeSpan.FromSeconds(5));

        items.Should().HaveCount(2);
        items[0].Command.Should().Be(MavCmd.NavTakeoff);
        items[1].Command.Should().Be(MavCmd.NavWaypoint);
        items[1].X.Should().Be(473977418);

        // Protocol should have sent: MISSION_REQUEST_LIST + MISSION_REQUEST x2 + MISSION_ACK
        transport.SentPackets.Should().HaveCountGreaterThanOrEqualTo(4);
        transport.SentPackets[0].MessageId.Should().Be(43); // MISSION_REQUEST_LIST
    }

    [Fact]
    public async Task DownloadMission_EmptyMission_ReturnsEmpty()
    {
        var transport = new ResQ.Mavlink.Tests.Infrastructure.InjectableTransport();
        var protocol = new MissionProtocol(transport);

        var downloadTask = protocol.DownloadMissionAsync(targetSystem: 1);

        await Task.Delay(30);

        transport.InjectPacket(MakePacket(new MissionCount { Count = 0, TargetSystem = 255, TargetComponent = 190 }, MissionCount.PayloadSize));
        transport.CompleteReceive();

        var items = await downloadTask.WaitAsync(TimeSpan.FromSeconds(5));

        items.Should().BeEmpty();
    }
}
