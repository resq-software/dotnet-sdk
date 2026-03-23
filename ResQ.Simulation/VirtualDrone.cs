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
/// Simulates a virtual drone that sends telemetry to HCE and reports detections.
/// </summary>
public class VirtualDrone
{
    // Simulation constants
    private const double BATTERY_DRAIN_PER_SECOND = 0.1;      // 0.1% per second = ~16 min flight time
    private const double LOW_BATTERY_THRESHOLD = 20.0;        // % battery to trigger RTL
    private const double CRITICAL_BATTERY_LEVEL = 5.0;        // % battery to force landing
    private const double DETECTION_PROBABILITY = 0.05;        // 5% chance of detection per second
    private const int TELEMETRY_INTERVAL_MS = 1000;           // 1 Hz telemetry rate
    private const int ERROR_RETRY_DELAY_MS = 1000;            // Retry delay after errors
    private const int LOG_EVERY_N_TELEMETRY = 10;             // Log telemetry every N packets

    // Movement constants
    private const double GPS_DELTA_DEGREES = 0.0001;          // ~11 meters at equator
    private const double ALTITUDE_DELTA_METERS = 2.0;         // Max altitude change per step
    private const double MIN_ALTITUDE_METERS = 10.0;          // Minimum safe altitude
    private const double MAX_ALTITUDE_METERS = 120.0;         // Maximum flight altitude

    // Detection constants
    private const double MIN_CONFIDENCE = 0.75;               // Minimum AI confidence
    private const double CONFIDENCE_RANGE = 0.25;             // Range for random confidence
    private const int FAKE_IMAGE_SIZE_BYTES = 1024;           // Size of generated fake images

    private readonly string _droneId;
    private readonly CoordinationHceClient _hce;
    private readonly InfrastructureApiClient _infra;

    // State
    private Location _location;
    private double _batteryPercent = 100.0;
    private string _flightMode = "IDLE";
    private volatile bool _isRunning = false;
    private int _telemetryCount = 0;
    private int _detectionCount = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualDrone"/> class.
    /// </summary>
    /// <param name="droneId">Unique identifier for this drone.</param>
    /// <param name="startLocation">Initial geographic position of the drone.</param>
    /// <param name="hce">Client for the coordination-hce service.</param>
    /// <param name="infra">Client for the infrastructure-api service.</param>
    /// <exception cref="ArgumentException">Thrown when droneId is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates or altitude are invalid.</exception>
    public VirtualDrone(
        string droneId,
        Location startLocation,
        CoordinationHceClient hce,
        InfrastructureApiClient infra)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(droneId, nameof(droneId));
        ArgumentNullException.ThrowIfNull(startLocation, nameof(startLocation));
        ArgumentNullException.ThrowIfNull(hce, nameof(hce));
        ArgumentNullException.ThrowIfNull(infra, nameof(infra));

        // Validate GPS coordinates
        if (startLocation.Latitude < -90.0 || startLocation.Latitude > 90.0)
            throw new ArgumentOutOfRangeException(
                nameof(startLocation.Latitude),
                startLocation.Latitude,
                "Latitude must be between -90 and 90 degrees");

        if (startLocation.Longitude < -180.0 || startLocation.Longitude > 180.0)
            throw new ArgumentOutOfRangeException(
                nameof(startLocation.Longitude),
                startLocation.Longitude,
                "Longitude must be between -180 and 180 degrees");

        if (startLocation.Altitude is < MIN_ALTITUDE_METERS or > MAX_ALTITUDE_METERS)
            throw new ArgumentOutOfRangeException(
                nameof(startLocation.Altitude),
                startLocation.Altitude,
                $"Altitude must be between {MIN_ALTITUDE_METERS}m and {MAX_ALTITUDE_METERS}m");

        _droneId = droneId;
        _location = startLocation;
        _hce = hce;
        _infra = infra;
    }

    /// <summary>
    /// Starts the drone's telemetry loop (sends data every 1 second).
    /// </summary>
    public async Task StartAsync(CancellationToken ct)
    {
        _isRunning = true;
        _flightMode = "ARMED";

        Console.WriteLine($"[{_droneId}] 🚁 Starting at ({_location.Latitude:F6}, {_location.Longitude:F6}, {_location.Altitude ?? 0:F1}m)");

        while (_isRunning && !ct.IsCancellationRequested)
        {
            try
            {
                // Simulate movement
                SimulateMovement();

                // Drain battery
                _batteryPercent -= BATTERY_DRAIN_PER_SECOND;

                // Send telemetry
                await SendTelemetryAsync(ct);

                // Random detection
                if (Random.Shared.NextDouble() < DETECTION_PROBABILITY)
                {
                    await SimulateDetectionAsync(ct);
                }

                // Check battery failsafe
                if (_batteryPercent < LOW_BATTERY_THRESHOLD && _flightMode != "RTL")
                {
                    Console.WriteLine($"[{_droneId}] ⚠️  LOW BATTERY! Returning to launch");
                    _flightMode = "RTL";
                }

                // Land if battery critical
                if (_batteryPercent < CRITICAL_BATTERY_LEVEL)
                {
                    _flightMode = "LAND";
                    _isRunning = false;
                }

                await Task.Delay(TELEMETRY_INTERVAL_MS, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"[{_droneId}] ❌ Error in main loop: {ex.Message}");
                await Task.Delay(ERROR_RETRY_DELAY_MS, ct); // Continue despite errors
            }
        }

        Console.WriteLine($"[{_droneId}] ✅ Stopped. Battery: {_batteryPercent:F1}% | Telemetry: {_telemetryCount} | Detections: {_detectionCount}");
    }

    private void SimulateMovement()
    {
        // Random walk
        var latDelta = (Random.Shared.NextDouble() - 0.5) * GPS_DELTA_DEGREES;
        var lonDelta = (Random.Shared.NextDouble() - 0.5) * GPS_DELTA_DEGREES;
        var altDelta = (Random.Shared.NextDouble() - 0.5) * ALTITUDE_DELTA_METERS;
        var currentAlt = _location.Altitude ?? MIN_ALTITUDE_METERS;

        _location = new Location(
            Math.Clamp(_location.Latitude + latDelta, -90.0, 90.0),
            Math.Clamp(_location.Longitude + lonDelta, -180.0, 180.0),
            Math.Clamp(currentAlt + altDelta, MIN_ALTITUDE_METERS, MAX_ALTITUDE_METERS)
        );
    }

    private async Task SendTelemetryAsync(CancellationToken ct)
    {
        var packet = new TelemetryPacket(
            DroneId: _droneId,
            Latitude: _location.Latitude,
            Longitude: _location.Longitude,
            Altitude: _location.Altitude ?? 0,
            Battery: _batteryPercent,
            FlightMode: _flightMode,
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );

        var batch = new TelemetryBatchRequest(
            DroneId: _droneId,
            Packets: new List<TelemetryPacket> { packet }
        );

        try
        {
            await _hce.SendTelemetryBatchAsync(batch, ct);
            _telemetryCount++;

            if (_telemetryCount % LOG_EVERY_N_TELEMETRY == 0)
            {
                Console.WriteLine($"[{_droneId}] 📡 Telemetry: ({_location.Latitude:F6},{_location.Longitude:F6}) | {_batteryPercent:F1}% | {_flightMode}");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"[{_droneId}] ❌ Telemetry failed: {ex.Message}");
        }
    }

    private async Task SimulateDetectionAsync(CancellationToken ct)
    {
        var detectionTypes = new[] { "FIRE", "FLOOD", "PERSON", "VEHICLE" };
        var type = detectionTypes[Random.Shared.Next(detectionTypes.Length)];
        var confidence = MIN_CONFIDENCE + (Random.Shared.NextDouble() * CONFIDENCE_RANGE);

        Console.WriteLine($"[{_droneId}] 🔥 DETECTION: {type} (confidence: {confidence:P0})");

        try
        {
            // 1. Upload evidence image to infrastructure-api
            var fakeImage = GenerateFakeImage(type);
            var fileName = $"{_droneId}_{type}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.jpg";
            var upload = await _infra.UploadImageAsync(fakeImage, fileName, ct);

            Console.WriteLine($"[{_droneId}] 📤 Evidence uploaded: {upload.Cid}");

            // 2. Record event on blockchain
            var evt = new BlockchainEventRequest(
                EventId: Guid.NewGuid().ToString(),
                EventType: $"{type}_DETECTED",
                Payload: System.Text.Json.JsonSerializer.Serialize(new
                {
                    droneId = _droneId,
                    type,
                    confidence,
                    location = _location
                }),
                IpfsCid: upload.Cid,
                DroneId: _droneId,
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );

            await _infra.RecordEventAsync(evt, ct);

            Console.WriteLine($"[{_droneId}] ⛓️  Blockchain event recorded");

            _detectionCount++;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"[{_droneId}] ❌ Detection processing failed: {ex.Message}");
        }
    }

    private byte[] GenerateFakeImage(string type)
    {
        // Generate a small fake JPEG with JPEG/JFIF header
        var header = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        var data = new byte[FAKE_IMAGE_SIZE_BYTES];
        Random.Shared.NextBytes(data);
        return header.Concat(data).ToArray();
    }

    /// <summary>
    /// Stops the drone's telemetry loop.
    /// </summary>
    /// <remarks>
    /// Signals the drone to stop sending telemetry. The drone will complete its current
    /// iteration and then exit the telemetry loop.
    /// </remarks>
    public void Stop() => _isRunning = false;
}
