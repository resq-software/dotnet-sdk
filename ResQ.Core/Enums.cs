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
/// Represents the operational status of a drone.
/// </summary>
/// <remarks>
/// These statuses represent the drone's current operational state within its
/// mission lifecycle. State transitions are typically managed by the flight
/// controller and mission management system.
/// </remarks>
/// <example>
/// <code>
/// if (drone.Status == DroneStatus.InFlight)
/// {
///     // Monitor mission progress
/// }
/// else if (drone.Status == DroneStatus.Emergency)
/// {
///     // Alert operators
/// }
/// </code>
/// </example>
public enum DroneStatus
{
    /// <summary>Drone is powered on and ready but not armed.</summary>
    Idle,

    /// <summary>Drone is armed and ready for takeoff.</summary>
    Armed,

    /// <summary>Drone is currently taking off.</summary>
    Takeoff,

    /// <summary>Drone is airborne and executing its mission.</summary>
    InFlight,

    /// <summary>Drone is returning to home/base location.</summary>
    Returning,

    /// <summary>Drone is currently landing.</summary>
    Landing,

    /// <summary>Drone has successfully landed.</summary>
    Landed,

    /// <summary>Drone has encountered an emergency situation.</summary>
    Emergency,

    /// <summary>Drone is offline or not responding.</summary>
    Offline
}

/// <summary>
/// Defines types of disasters and emergencies the ResQ system can respond to.
/// </summary>
/// <remarks>
/// These disaster types are used to categorize incidents, trigger appropriate
/// response protocols, and route alerts to specialized response teams.
/// </remarks>
/// <example>
/// <code>
/// switch (incident.DisasterType)
/// {
///     case DisasterType.Wildfire:
///         await DeployFireSuppressionDrones(incident.Location);
///         break;
///     case DisasterType.Flood:
///         await DeploySearchAndRescueDrones(incident.Location);
///         break;
/// }
/// </code>
/// </example>
public enum DisasterType
{
    /// <summary>No disaster or not classified.</summary>
    None,

    /// <summary>Flooding or flash flood event.</summary>
    Flood,

    /// <summary>Wildfire or forest fire.</summary>
    Wildfire,

    /// <summary>Earthquake or seismic event.</summary>
    Earthquake,

    /// <summary>Hurricane, typhoon, or tropical cyclone.</summary>
    Hurricane,

    /// <summary>Tsunami or tidal wave.</summary>
    Tsunami,

    /// <summary>Structural collapse of buildings or infrastructure.</summary>
    StructuralCollapse,

    /// <summary>Chemical spill or hazardous material release.</summary>
    ChemicalSpill
}

/// <summary>
/// Defines types of objects and phenomena detectable by AI systems.
/// </summary>
/// <remarks>
/// These detection types represent what the ResQ AI vision systems can identify
/// from drone sensor data. They are used for automated alerting and mission
/// prioritization.
/// </remarks>
/// <example>
/// <code>
/// if (detection.Type == DetectionType.Fire &amp;&amp; detection.Confidence > 0.9)
/// {
///     await TriggerAlert(AlertSeverity.Critical, detection.Location);
/// }
/// </code>
/// </example>
public enum DetectionType
{
    /// <summary>No detection or unknown.</summary>
    None,

    /// <summary>Fire, flames, or smoke detected.</summary>
    Fire,

    /// <summary>Flood water detected.</summary>
    Flood,

    /// <summary>Human person detected.</summary>
    Person,

    /// <summary>Vehicle detected.</summary>
    Vehicle,

    /// <summary>Structural damage detected.</summary>
    StructuralDamage,

    /// <summary>Smoke plume detected.</summary>
    SmokePlume,

    /// <summary>Rising water level detected.</summary>
    WaterLevelRise
}

/// <summary>
/// Defines types of blockchain events for immutable logging.
/// </summary>
/// <remarks>
/// These event types categorize the various events that are recorded on the
/// Neo N3 blockchain for audit trails and verification.
/// </remarks>
/// <example>
/// <code>
/// var evt = new BlockchainEvent
/// {
///     EventType = BlockchainEventType.IncidentDetected,
///     Timestamp = DateTimeOffset.UtcNow,
///     Location = incident.Location
/// };
/// await neoClient.RecordEventAsync(evt);
/// </code>
/// </example>
public enum BlockchainEventType
{
    /// <summary>Unspecified or unknown event type.</summary>
    Unspecified,

    /// <summary>New incident detected by sensors or AI.</summary>
    IncidentDetected,

    /// <summary>Incident has been verified by human operators.</summary>
    IncidentVerified,

    /// <summary>Drone mission has started.</summary>
    MissionStarted,

    /// <summary>Drone mission has completed.</summary>
    MissionCompleted,

    /// <summary>Supply delivery has been confirmed.</summary>
    DeliveryConfirmed,

    /// <summary>Drone location has been attested on blockchain.</summary>
    LocationAttestation,

    /// <summary>Evidence has been submitted to storage and blockchain.</summary>
    EvidenceSubmitted,

    /// <summary>Pre-alert issued by predictive system.</summary>
    PreAlertIssued,

    /// <summary>Drone swarm has been deployed.</summary>
    SwarmDeployment
}

/// <summary>
/// Represents the status of a blockchain transaction.
/// </summary>
/// <remarks>
/// Transaction status tracks the lifecycle of a transaction from submission
/// through confirmation on the blockchain.
/// </remarks>
/// <example>
/// <code>
/// var result = await neoClient.RecordEventAsync(evt);
/// if (result.Status == TransactionStatus.Confirmed)
/// {
///     Console.WriteLine($"Confirmed in block {result.BlockHeight}");
/// }
/// </code>
/// </example>
public enum TransactionStatus
{
    /// <summary>Transaction submitted but not yet confirmed.</summary>
    Pending,

    /// <summary>Transaction has been confirmed on the blockchain.</summary>
    Confirmed,

    /// <summary>Transaction failed or was rejected.</summary>
    Failed
}

/// <summary>
/// Defines alert severity levels for prioritization.
/// </summary>
/// <remarks>
/// Severity levels determine response urgency and notification routing.
/// They can be derived from confidence scores or risk assessments.
/// </remarks>
/// <example>
/// <code>
/// // Convert confidence to severity
/// var severity = detection.Confidence switch
/// {
///     >= 0.95 => AlertSeverity.Critical,
///     >= 0.85 => AlertSeverity.High,
///     >= 0.70 => AlertSeverity.Medium,
///     _ => AlertSeverity.Low
/// };
/// </code>
/// </example>
public enum AlertSeverity
{
    /// <summary>Low priority - routine information.</summary>
    Low,

    /// <summary>Medium priority - notable event.</summary>
    Medium,

    /// <summary>High priority - significant issue.</summary>
    High,

    /// <summary>Critical priority - immediate action required.</summary>
    Critical
}
