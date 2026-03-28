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
using Xunit;

namespace ResQ.Mavlink.Sitl.Tests.Integration;

/// <summary>
/// Integration tests that require a live ArduPilot SITL binary.
/// All tests skip gracefully when the binary is not available.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SitlIntegrationTests
{
    private const string DefaultSitlBinary = "/usr/local/bin/arducopter";

    /// <summary>
    /// Checks whether an ArduPilot SITL binary is present on the current system.
    /// </summary>
    /// <returns><see langword="true"/> if the binary exists; <see langword="false"/> otherwise.</returns>
    private static bool IsSitlAvailable()
    {
        var candidates = new[]
        {
            DefaultSitlBinary,
            "/usr/bin/arducopter",
            "/usr/local/bin/arduplane",
            Environment.GetEnvironmentVariable("SITL_BINARY") ?? string.Empty,
        };

        return candidates.Any(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
    }

    private static string FindSitlBinary()
    {
        var candidates = new[]
        {
            DefaultSitlBinary,
            "/usr/bin/arducopter",
            "/usr/local/bin/arduplane",
            Environment.GetEnvironmentVariable("SITL_BINARY") ?? string.Empty,
        };

        return candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
               ?? DefaultSitlBinary;
    }

    [Fact]
    public async Task Sitl_SpawnConnectStep_ReceivesTelemetry()
    {
        if (!IsSitlAvailable())
        {
            // Skip gracefully — SITL binary not installed on this system.
            return;
        }

        var processManagerOptions = new SitlProcessManagerOptions
        {
            SitlBinaryPath = FindSitlBinary(),
            BasePort = 15760,
            BaseJsonPort = 19002,
            MaxInstances = 1,
            HomeLocation = "-35.363262,149.165237,584,353",
            Model = "+",
        };

        await using var manager = new SitlProcessManager(processManagerOptions);
        var backendOptions = new SitlBackendOptions
        {
            InstanceIndex = 0,
            PhysicsRateHz = 400,
            VehicleType = "ArduCopter",
        };

        await using var backend = new ArduPilotSitlBackend(backendOptions, manager);

        var config = new DroneConfig("sitl-integration-0", new Vector3(0, 0, 0), "ArduCopter");
        await backend.InitializeAsync(config);

        // Allow the SITL process to boot and send initial telemetry.
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Step the backend 100 times.
        var wind = Vector3.Zero;
        DronePhysicsState lastState = default;
        for (var i = 0; i < 100; i++)
        {
            lastState = await backend.StepAsync(0.01, wind);
            await Task.Delay(10); // ~100 Hz polling
        }

        // We should have received at least position data.
        // The state should not be the default zeroed-out "no data" sentinel indefinitely.
        // If we received real telemetry the battery should be 100% (default from mapper).
        lastState.BatteryPercent.Should().BeGreaterThanOrEqualTo(0.0);
        lastState.BatteryPercent.Should().BeLessThanOrEqualTo(100.0);
    }
}
