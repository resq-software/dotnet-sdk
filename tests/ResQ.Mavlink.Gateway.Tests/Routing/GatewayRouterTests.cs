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
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Gateway.Routing;
using Xunit;

namespace ResQ.Mavlink.Gateway.Tests.Routing;

public class GatewayRouterTests
{
    private static GatewayRouter MakeRouter(Action<GatewayRoutingOptions>? configure = null)
    {
        var opts = new GatewayRoutingOptions();
        configure?.Invoke(opts);
        return new GatewayRouter(Options.Create(opts));
    }

    // ── Internal-only filter ─────────────────────────────────────────────────

    [Fact]
    public void ShouldForwardToResq_HeartbeatId0_ReturnsFalse()
    {
        var router = MakeRouter();

        router.ShouldForwardToResq(1, messageId: 0).Should().BeFalse();
    }

    [Fact]
    public void ShouldForwardToResq_TelemetryMessage_NotInInternalList_ReturnsTrue()
    {
        var router = MakeRouter();

        // GlobalPositionInt = 33, not in internal list
        router.ShouldForwardToResq(1, messageId: 33).Should().BeTrue();
    }

    [Fact]
    public void ShouldForwardToResq_CustomInternalOnlyId_ReturnsFalse()
    {
        var router = MakeRouter(o => o.InternalOnlyMessageIds = [0, 99]);

        router.ShouldForwardToResq(1, messageId: 99).Should().BeFalse();
    }

    // ── Unknown message IDs ──────────────────────────────────────────────────

    [Fact]
    public void ShouldForwardToResq_UnknownMessageId_ForwardUnknownTrue_ReturnsTrue()
    {
        var router = MakeRouter(o => o.ForwardUnknownMessages = true);

        router.ShouldForwardToResq(1, messageId: 9999).Should().BeTrue();
    }

    // ── Rate limiting ────────────────────────────────────────────────────────

    [Fact]
    public void ShouldForwardToResq_WithinRateLimit_ReturnsTrue()
    {
        var router = MakeRouter(o => o.TelemetryRateLimitHz = 5);

        // Send 5 messages (within cap) and record each — all should be allowed.
        for (var i = 0; i < 5; i++)
        {
            router.ShouldForwardToResq(1, 33).Should().BeTrue($"message {i} should be within limit");
            router.RecordForwarded(1);
        }
    }

    [Fact]
    public void ShouldForwardToResq_ExceedingRateLimit_ReturnsFalse()
    {
        var router = MakeRouter(o => o.TelemetryRateLimitHz = 3);

        // Fill the window.
        for (var i = 0; i < 3; i++)
        {
            router.ShouldForwardToResq(1, 33).Should().BeTrue();
            router.RecordForwarded(1);
        }

        // The 4th attempt within the same second must be rejected.
        router.ShouldForwardToResq(1, 33).Should().BeFalse("rate limit of 3 Hz is exhausted");
    }

    [Fact]
    public void ShouldForwardToResq_RateLimitIsPerVehicle_OtherVehicleNotAffected()
    {
        var router = MakeRouter(o => o.TelemetryRateLimitHz = 2);

        // Exhaust vehicle 1.
        for (var i = 0; i < 2; i++)
        {
            router.ShouldForwardToResq(1, 33);
            router.RecordForwarded(1);
        }
        router.ShouldForwardToResq(1, 33).Should().BeFalse();

        // Vehicle 2 is unaffected.
        router.ShouldForwardToResq(2, 33).Should().BeTrue();
    }

    // ── RecordForwarded ──────────────────────────────────────────────────────

    [Fact]
    public void RecordForwarded_DoesNotThrowForUnseenVehicle()
    {
        var router = MakeRouter();
        var act = () => router.RecordForwarded(42);

        act.Should().NotThrow();
    }
}
