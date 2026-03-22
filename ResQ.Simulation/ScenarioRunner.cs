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

using ResQ.Clients;
using Location = ResQ.Core.Location;

namespace ResQ.Simulation;

/// <summary>
/// Orchestrates simulation scenarios to stress-test ResQ services.
/// </summary>
public class ScenarioRunner
{
    // Validation constants
    private const int MIN_DRONE_COUNT = 1;
    private const int MAX_DRONE_COUNT = 10000;
    private const double MIN_LATITUDE = -90.0;
    private const double MAX_LATITUDE = 90.0;
    private const double MIN_LONGITUDE = -180.0;
    private const double MAX_LONGITUDE = 180.0;
    private const double MIN_ALTITUDE = 10.0; // N19: matches VirtualDrone.MIN_ALTITUDE_METERS to prevent false-valid then constructor crash
    private const double MAX_ALTITUDE = 120.0; // N20: matches VirtualDrone.MAX_ALTITUDE_METERS to prevent ValidateLocation passing altitudes that would crash VirtualDrone constructor

    private readonly CoordinationHceClient _hce;
    private readonly InfrastructureApiClient _infra;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioRunner"/> class.
    /// </summary>
    /// <param name="hce">Client for the coordination-hce service.</param>
    /// <param name="infra">Client for the infrastructure-api service.</param>
    public ScenarioRunner(CoordinationHceClient hce, InfrastructureApiClient infra)
    {
        ArgumentNullException.ThrowIfNull(hce, nameof(hce));
        ArgumentNullException.ThrowIfNull(infra, nameof(infra));

        _hce = hce;
        _infra = infra;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioRunner"/> class using service URLs.
    /// </summary>
    /// <param name="hceUrl">Base URL of the coordination-hce service.</param>
    /// <param name="infraUrl">Base URL of the infrastructure-api service.</param>
    public ScenarioRunner(string hceUrl = "http://localhost:3000", string infraUrl = "http://localhost:5000")
    {
        ArgumentNullException.ThrowIfNull(hceUrl, nameof(hceUrl));
        ArgumentNullException.ThrowIfNull(infraUrl, nameof(infraUrl));

        if (!Uri.TryCreate(hceUrl, UriKind.Absolute, out _))
            throw new ArgumentException("Invalid HCE URL format", nameof(hceUrl));

        if (!Uri.TryCreate(infraUrl, UriKind.Absolute, out _))
            throw new ArgumentException("Invalid infrastructure API URL format", nameof(infraUrl));

        _hce = new CoordinationHceClient(hceUrl);
        _infra = new InfrastructureApiClient(infraUrl);
    }

    /// <summary>
    /// Validates drone count parameter is within safe bounds.
    /// </summary>
    /// <param name="droneCount">The number of drones to validate.</param>
    /// <param name="paramName">Name of the parameter for error messages.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when drone count is outside valid range.</exception>
    private static void ValidateDroneCount(int droneCount, string paramName)
    {
        if (droneCount < MIN_DRONE_COUNT)
            throw new ArgumentOutOfRangeException(paramName, droneCount,
                $"Drone count must be at least {MIN_DRONE_COUNT}");

        if (droneCount > MAX_DRONE_COUNT)
            throw new ArgumentOutOfRangeException(paramName, droneCount,
                $"Drone count cannot exceed {MAX_DRONE_COUNT} (would cause excessive load)");
    }

    /// <summary>
    /// Validates location coordinates are within valid GPS bounds.
    /// </summary>
    /// <param name="location">The location to validate.</param>
    /// <param name="paramName">Name of the parameter for error messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when location is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are outside valid ranges.</exception>
    private static void ValidateLocation(Location location, string paramName)
    {
        ArgumentNullException.ThrowIfNull(location, paramName);

        if (location.Latitude < MIN_LATITUDE || location.Latitude > MAX_LATITUDE)
            throw new ArgumentOutOfRangeException(paramName,
                $"Latitude must be between {MIN_LATITUDE} and {MAX_LATITUDE}");

        if (location.Longitude < MIN_LONGITUDE || location.Longitude > MAX_LONGITUDE)
            throw new ArgumentOutOfRangeException(paramName,
                $"Longitude must be between {MIN_LONGITUDE} and {MAX_LONGITUDE}");

        if (location.Altitude < MIN_ALTITUDE || location.Altitude > MAX_ALTITUDE)
            throw new ArgumentOutOfRangeException(paramName,
                $"Altitude must be between {MIN_ALTITUDE}m and {MAX_ALTITUDE}m");
    }

    /// <summary>
    /// Scenario 1: Single drone survey mission (2 minutes).
    /// Tests basic telemetry, detection, and blockchain recording.
    /// </summary>
    public async Task RunSingleDroneSurveyAsync()
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  Scenario 1: Single Drone Survey (2 min)                    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        var drone = new VirtualDrone(
            "drone-survey-01",
            new Location(37.7749, -122.4194, 50.0),
            _hce,
            _infra
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await drone.StartAsync(cts.Token);

        Console.WriteLine("\n✅ Single drone survey complete\n");
    }

    /// <summary>
    /// Scenario 2: Swarm of N drones coordinated search (5 minutes).
    /// Tests coordination and concurrent telemetry.
    /// </summary>
    /// <param name="droneCount">Number of drones to simulate (1-10,000)</param>
    public async Task RunSwarmSurveyAsync(int droneCount = 10, CancellationToken ct = default)
    {
        ValidateDroneCount(droneCount, nameof(droneCount));

        Console.WriteLine($"\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  Scenario 2: {droneCount}-Drone Swarm Survey (5 min)");
        Console.WriteLine($"╚══════════════════════════════════════════════════════════════╝\n");

        var tasks = new List<Task>();

        for (int i = 0; i < droneCount; i++)
        {
            var drone = new VirtualDrone(
                $"drone-swarm-{i:D2}",
                new Location(
                    37.7749 + (i * 0.001), // Spread out drones
                    -122.4194 + (i * 0.001),
                    50.0
                ),
                _hce,
                _infra
            );

            var task = Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                await drone.StartAsync(cts.Token);
            });

            tasks.Add(task);

            // Stagger starts (100ms between each drone)
            await Task.Delay(100, ct);
        }

        await Task.WhenAll(tasks);

        Console.WriteLine($"\n✅ {droneCount}-drone swarm complete\n");
    }

    /// <summary>
    /// Scenario 3: Stress test with many drones (3 minutes).
    /// Tests system limits and concurrent load handling.
    /// </summary>
    /// <param name="droneCount">Number of drones to simulate (1-10,000)</param>
    public async Task RunStressTestAsync(int droneCount = 100, CancellationToken ct = default)
    {
        ValidateDroneCount(droneCount, nameof(droneCount));

        Console.WriteLine($"\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  Scenario 3: STRESS TEST ({droneCount} Drones, 3 min)");
        Console.WriteLine($"╚══════════════════════════════════════════════════════════════╝\n");

        var semaphore = new SemaphoreSlim(50); // Max 50 concurrent
        var tasks = new List<Task>();

        Console.WriteLine($"Spawning {droneCount} drones (max 50 concurrent)...\n");

        for (int i = 0; i < droneCount; i++)
        {
            await semaphore.WaitAsync();

            var droneId = $"stress-{i:D3}";
            var capturedI = i; // capture loop variable before Task.Run to avoid closure bug
            var task = Task.Run(async () =>
            {
                try
                {
                    var drone = new VirtualDrone(
                        droneId,
                        new Location(
                            37.77 + (capturedI % 10) * 0.01,
                            -122.41 + (capturedI / 10) * 0.01,
                            50.0
                        ),
                        _hce,
                        _infra
                    );

                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                    await drone.StartAsync(cts.Token);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            tasks.Add(task);

            // Stagger starts (100ms = 10 drones/sec)
            await Task.Delay(100, ct);
        }

        await Task.WhenAll(tasks);

        Console.WriteLine($"\n✅ STRESS TEST COMPLETE ({droneCount} drones)\n");
    }

    /// <summary>
    /// Scenario 4: Incident flood - all drones detect same incident (30 seconds).
    /// Tests spike handling when many drones report simultaneously.
    /// </summary>
    /// <param name="droneCount">Number of drones to simulate (1-10,000)</param>
    public async Task RunIncidentFloodAsync(int droneCount = 20)
    {
        ValidateDroneCount(droneCount, nameof(droneCount));

        Console.WriteLine($"\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  Scenario 4: Incident Flood ({droneCount} drones, same location)");
        Console.WriteLine($"╚══════════════════════════════════════════════════════════════╝\n");

        // All drones converge on same incident location
        var incidentLocation = new Location(37.7749, -122.4194, 50.0);
        ValidateLocation(incidentLocation, nameof(incidentLocation));

        var tasks = Enumerable.Range(0, droneCount).Select(i =>
        {
            var drone = new VirtualDrone(
                $"flood-{i:D2}",
                incidentLocation, // All at same spot
                _hce,
                _infra
            );

            return Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await drone.StartAsync(cts.Token);
            });
        });

        await Task.WhenAll(tasks);

        Console.WriteLine($"\n✅ Incident flood complete ({droneCount} drones)\n");
    }

    /// <summary>
    /// Checks if services are healthy before running scenarios.
    /// </summary>
    /// <returns>True if all services are healthy; false otherwise.</returns>
    /// <remarks>
    /// Verifies connectivity to both coordination-hce and infrastructure-api services.
    /// Outputs health status to console.
    /// </remarks>
    public async Task<bool> CheckServicesAsync(CancellationToken ct = default)
    {
        Console.WriteLine("Checking service health...");

        try
        {
            var hceHealth = await _hce.GetHealthAsync(ct);
            Console.WriteLine($"  ✅ coordination-hce: {hceHealth.Status}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"  ❌ coordination-hce: {ex.Message}");
            return false;
        }

        try
        {
            var infraHealth = await _infra.GetHealthAsync(ct);
            Console.WriteLine($"  ✅ infrastructure-api: {infraHealth.Status}");
            Console.WriteLine($"     - Pinata: {(infraHealth.Pinata ? "✅" : "❌")}");
            Console.WriteLine($"     - Gemini: {(infraHealth.Gemini ? "✅" : "❌")}");
            Console.WriteLine($"     - Blockchain: {(infraHealth.Blockchain ? "✅" : "❌")}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"  ❌ infrastructure-api: {ex.Message}");
            return false;
        }

        // Authenticate with infrastructure-api for protected endpoints (upload, blockchain)
        var infraUser = Environment.GetEnvironmentVariable("INFRA_ADMIN_USERNAME");
        var infraPass = Environment.GetEnvironmentVariable("INFRA_ADMIN_PASSWORD");

        if (string.IsNullOrEmpty(infraUser) || string.IsNullOrEmpty(infraPass))
        {
            Console.WriteLine("  ❌ infrastructure-api auth: INFRA_ADMIN_USERNAME and INFRA_ADMIN_PASSWORD must be set");
            Console.WriteLine();
            return false;
        }

        var authed = await _infra.AuthenticateAsync(infraUser, infraPass, ct);
        Console.WriteLine($"  {(authed ? "✅" : "❌")} infrastructure-api auth: {(authed ? "JWT acquired" : "failed")}");

        if (!authed)
        {
            Console.WriteLine();
            return false;
        }

        Console.WriteLine();
        return true;
    }
}
