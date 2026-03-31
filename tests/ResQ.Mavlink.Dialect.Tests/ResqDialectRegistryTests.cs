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
using ResQ.Mavlink.Dialect.Messages;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;
using Xunit;

namespace ResQ.Mavlink.Dialect.Tests;

/// <summary>
/// Tests verifying that <see cref="ResqDialectRegistry.Register"/> correctly wires the dialect
/// into the MAVLink codec and message registry tables.
/// </summary>
public sealed class ResqDialectRegistryTests
{
    static ResqDialectRegistryTests() => ResqDialectRegistry.Register();

    [Theory]
    [InlineData(60000u)]
    [InlineData(60001u)]
    [InlineData(60002u)]
    [InlineData(60003u)]
    [InlineData(60004u)]
    [InlineData(60005u)]
    [InlineData(60006u)]
    [InlineData(60007u)]
    public void Register_AllDialectMessageIds_AreKnownToCrcTable(uint messageId)
    {
        MavlinkCrc.GetCrcExtra(messageId).Should().NotBeNull(
            $"message ID {messageId} should have a CRC extra after Register()");
    }

    [Theory]
    [InlineData(60000u)]
    [InlineData(60001u)]
    [InlineData(60002u)]
    [InlineData(60003u)]
    [InlineData(60004u)]
    [InlineData(60005u)]
    [InlineData(60006u)]
    [InlineData(60007u)]
    public void Register_AllDialectMessageIds_AreRegisteredInMessageRegistry(uint messageId)
    {
        MessageRegistry.IsRegistered(messageId).Should().BeTrue(
            $"message ID {messageId} should be registered in MessageRegistry after Register()");
    }

    [Fact]
    public void Register_IsIdempotent_DoesNotThrow()
    {
        var act = () =>
        {
            ResqDialectRegistry.Register();
            ResqDialectRegistry.Register();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void Register_Detection_DeserializesCorrectType()
    {
        var original = new ResqDetection { Confidence = 77, TimestampMs = 9999UL };
        var buf = new byte[ResqDetection.PayloadSize];
        original.Serialize(buf);

        var ok = MessageRegistry.TryDeserialize(60000, buf, out var msg);

        ok.Should().BeTrue();
        msg.Should().BeOfType<ResqDetection>();
        ((ResqDetection)msg!).Confidence.Should().Be(77);
    }

    [Fact]
    public void Register_EmergencyBeacon_DeserializesCorrectType()
    {
        var original = new ResqEmergencyBeacon { Ttl = 3, BeaconId = 123u };
        var buf = new byte[ResqEmergencyBeacon.PayloadSize];
        original.Serialize(buf);

        var ok = MessageRegistry.TryDeserialize(60007, buf, out var msg);

        ok.Should().BeTrue();
        msg.Should().BeOfType<ResqEmergencyBeacon>();
        ((ResqEmergencyBeacon)msg!).Ttl.Should().Be(3);
    }
}
