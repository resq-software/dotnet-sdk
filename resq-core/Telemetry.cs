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

namespace ResQ.Core;

/// <summary>
/// Represents a complete telemetry packet from a drone.
/// </summary>
/// <remarks>
/// This record contains comprehensive telemetry data from a drone including
/// position, velocity, status, battery levels, sensor health, and mission
/// progress. It is used for real-time monitoring and logging.
/// </remarks>
/// <example>
/// <code>
/// var telemetry = new TelemetryPacket
/// {
///     DroneId = "drn-001",
///     SequenceNumber = 12345,
///     Position = new Location(37.7749, -122.4194, 100.0),
///     Velocity = new Velocity(10.0, 5.0, -1.0),
///     Status = DroneStatus.InFlight,
///     BatteryPercent = 75.5f,
///     Detections = new List&lt;Detection&gt;()
/// };
/// </code>
/// </example>
public record TelemetryPacket
{
    /// <summary>Unique identifier for the drone.</summary>
    public required string DroneId { get; init; }

    /// <summary>Optional swarm identifier if the drone is part of a swarm.</summary>
    public string? SwarmId { get; init; }

    /// <summary>Sequence number for ordering telemetry packets.</summary>
    public ulong SequenceNumber { get; init; }

    /// <summary>UTC timestamp when the telemetry was recorded.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Current geographic position of the drone.</summary>
    public required Location Position { get; init; }

    /// <summary>Current velocity vector in NED frame.</summary>
    public Velocity? Velocity { get; init; }

    /// <summary>Current operational status.</summary>
    public DroneStatus Status { get; init; } = DroneStatus.Idle;

    /// <summary>Battery level as percentage (0-100).</summary>
    public float BatteryPercent { get; init; }

    /// <summary>Battery voltage in volts.</summary>
    public float BatteryVoltage { get; init; }

    /// <summary>True if GPS is functioning normally.</summary>
    public bool GpsOk { get; init; } = true;

    /// <summary>True if IMU is functioning normally.</summary>
    public bool ImuOk { get; init; } = true;

    /// <summary>True if camera is functioning normally.</summary>
    public bool CameraOk { get; init; } = true;

    /// <summary>True if thermal sensor is functioning normally.</summary>
    public bool ThermalOk { get; init; } = true;

    /// <summary>ID of the current mission, if any.</summary>
    public string? CurrentMissionId { get; init; }

    /// <summary>Mission completion percentage (0-100).</summary>
    public float MissionProgressPercent { get; init; }

    /// <summary>List of AI detections from this telemetry packet.</summary>
    public List<Detection> Detections { get; init; } = new();
}

/// <summary>
/// Represents an AI detection result from drone sensors.
/// </summary>
/// <remarks>
/// Detection records contain information about objects or phenomena identified
/// by the AI vision system, including location, confidence, and evidence links.
/// </remarks>
/// <example>
/// <code>
/// var detection = new Detection
/// {
///     DetectionId = "det-001",
///     Type = DetectionType.Fire,
///     Confidence = 0.95f,
///     Location = new Location(37.7749, -122.4194),
///     EvidenceCid = "Qmabc123..."
/// };
/// </code>
/// </example>
public record Detection
{
    /// <summary>Unique identifier for this detection.</summary>
    public required string DetectionId { get; init; }

    /// <summary>Type of object or phenomenon detected.</summary>
    public DetectionType Type { get; init; }

    /// <summary>AI confidence score (0.0 to 1.0).</summary>
    public float Confidence { get; init; }

    /// <summary>Geographic location of the detection.</summary>
    public required Location Location { get; init; }

    /// <summary>UTC timestamp when the detection was made.</summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>True if evidence has been uploaded to storage.</summary>
    public bool EvidenceUploaded { get; init; }

    /// <summary>IPFS CID of the evidence file, if uploaded.</summary>
    public string? EvidenceCid { get; init; }
}
