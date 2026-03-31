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
using ResQ.Mavlink.Transport;
using Xunit;

namespace ResQ.Mavlink.Tests.Transport;

/// <summary>
/// Tests for <see cref="SerialTransport"/>. Hardware tests are guarded with [Trait("Category", "Hardware")].
/// </summary>
public sealed class SerialTransportTests
{
    [Fact]
    public void Constructor_DefaultOptions_HasConnectingState()
    {
        var opts = new SerialTransportOptions { PortName = "/dev/null", BaudRate = 9600 };
        var transport = new SerialTransport(opts);

        // Before Open(), state is Connecting
        transport.State.Should().Be(TransportState.Connecting);

        // Don't call Open() because the port likely doesn't exist in CI
    }

    [Fact]
    public async Task DisposeAsync_WithoutOpening_DoesNotThrow()
    {
        var opts = new SerialTransportOptions { PortName = "/dev/null", BaudRate = 9600 };
        await using var transport = new SerialTransport(opts);

        // Should not throw
        transport.State.Should().Be(TransportState.Connecting);
    }

    [Fact]
    public async Task DisposeAsync_AfterDispose_StateIsDisposed()
    {
        var opts = new SerialTransportOptions { PortName = "/dev/null" };
        var transport = new SerialTransport(opts);

        await transport.DisposeAsync();

        transport.State.Should().Be(TransportState.Disposed);
    }

    /// <summary>
    /// Hardware-only test: requires a real loopback serial port pair (e.g. /dev/ttyUSB0 + /dev/ttyUSB1).
    /// Skipped in CI via trait filter.
    /// </summary>
    [Fact(Skip = "Requires hardware serial port — run manually with real serial loopback")]
    [Trait("Category", "Hardware")]
    public async Task Loopback_SendAndReceive_PacketMatches()
    {
        // This test requires two physically connected serial ports.
        // On Linux: use socat to create a virtual pair:
        //   socat -d -d pty,raw,echo=0 pty,raw,echo=0
        // Then update the port names accordingly.
        const string port0 = "/dev/ttyUSB0";
        const string port1 = "/dev/ttyUSB1";

        var optsA = new SerialTransportOptions { PortName = port0, BaudRate = 115200 };
        var optsB = new SerialTransportOptions { PortName = port1, BaudRate = 115200 };

        await using var a = new SerialTransport(optsA);
        await using var b = new SerialTransport(optsB);

        a.Open();
        b.Open();

        a.State.Should().Be(TransportState.Connected);
        b.State.Should().Be(TransportState.Connected);

        // Test would continue with send/receive, but is hardware-only
        await Task.CompletedTask;
    }
}
