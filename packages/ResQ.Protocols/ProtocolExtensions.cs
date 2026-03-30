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

namespace ResQ.Protocols;

/// <summary>
/// Provides extension methods for working with protobuf-generated types and timestamps.
/// </summary>
/// <remarks>
/// This static class contains utility extension methods that simplify working with
/// protocol buffer generated types, particularly for timestamp conversions between
/// Unix milliseconds and .NET <see cref="DateTimeOffset"/> types.
/// 
/// <para>
/// These extensions are commonly used when serializing/deserializing gRPC messages
/// and when interfacing with systems that use Unix timestamps.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Convert Unix timestamp from protobuf message
/// var timestamp = protoMessage.TimestampMs.FromUnixMs();
/// Console.WriteLine($"Event occurred at: {timestamp}");
/// 
/// // Convert to Unix timestamp for protobuf
/// var now = DateTimeOffset.UtcNow;
/// protoMessage.TimestampMs = now.ToUnixMs();
/// </code>
/// </example>
public static class ProtocolExtensions
{
    /// <summary>
    /// Converts a Unix timestamp in milliseconds to a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="timestampMs">The Unix timestamp in milliseconds since January 1, 1970 UTC.</param>
    /// <returns>A <see cref="DateTimeOffset"/> representing the specified timestamp.</returns>
    /// <remarks>
    /// This method handles the conversion from Unix time (common in protobuf and JSON APIs)
    /// to .NET's <see cref="DateTimeOffset"/> type. The resulting value has UTC as its offset.
    /// </remarks>
    /// <example>
    /// <code>
    /// // From a protobuf timestamp field
    /// long timestampMs = 1704067200000; // 2024-01-01 00:00:00 UTC
    /// var dateTime = timestampMs.FromUnixMs();
    /// Console.WriteLine(dateTime); // 2024-01-01 00:00:00 +00:00
    /// 
    /// // From current time
    /// var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().FromUnixMs();
    /// </code>
    /// </example>
    public static DateTimeOffset FromUnixMs(this long timestampMs)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);
    }

    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> to a Unix timestamp in milliseconds.
    /// </summary>
    /// <param name="dateTime">The date and time to convert.</param>
    /// <returns>The Unix timestamp in milliseconds since January 1, 1970 UTC.</returns>
    /// <remarks>
    /// This method converts a <see cref="DateTimeOffset"/> to Unix time in milliseconds,
    /// which is commonly used in protobuf messages, JSON APIs, and JavaScript interop.
    /// The conversion accounts for the offset and returns the UTC-based timestamp.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Convert current time
    /// var now = DateTimeOffset.UtcNow;
    /// var timestamp = now.ToUnixMs();
    /// 
    /// // Set protobuf timestamp field
    /// protoMessage.TimestampMs = timestamp;
    /// 
    /// // Convert specific time
    /// var eventTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    /// var eventTimestamp = eventTime.ToUnixMs(); // 1704067200000
    /// </code>
    /// </example>
    public static long ToUnixMs(this DateTimeOffset dateTime)
    {
        return dateTime.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Creates a Unix timestamp in milliseconds for the current UTC time.
    /// </summary>
    /// <returns>The current UTC time as a Unix timestamp in milliseconds.</returns>
    /// <remarks>
    /// This is a convenience method equivalent to <c>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()</c>.
    /// It's commonly used when setting timestamp fields in protobuf messages.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set current timestamp in protobuf message
    /// protoMessage.TimestampMs = ProtocolExtensions.NowUnixMs();
    /// 
    /// // Or use as a static import
    /// using static ResQ.Protocols.ProtocolExtensions;
    /// protoMessage.TimestampMs = NowUnixMs();
    /// </code>
    /// </example>
    public static long NowUnixMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

/// <summary>
/// Defines severity levels for alerts and events in the ResQ system.
/// </summary>
/// <remarks>
/// These severity levels are used throughout the system to classify the urgency
/// and importance of alerts, incidents, and events. They map to protocol buffer
/// enum values and are ordered from lowest (Low) to highest (Critical) severity.
/// 
/// <para>
/// When converting from risk scores or confidence values, use the following thresholds:
/// <list type="bullet">
/// <item><description>Critical: Risk score >= 0.9 or confidence >= 0.95</description></item>
/// <item><description>High: Risk score >= 0.75 or confidence >= 0.85</description></item>
/// <item><description>Medium: Risk score >= 0.6 or confidence >= 0.7</description></item>
/// <item><description>Low: Risk score &lt; 0.6 or confidence &lt; 0.7</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create an alert with severity
/// var alert = new Alert
/// {
///     Severity = AlertSeverity.High,
///     Message = "Fire detected in sector 7"
/// };
/// 
/// // Convert from risk score
/// double riskScore = 0.85;
/// var severity = riskScore >= 0.9 ? AlertSeverity.Critical :
///                riskScore >= 0.75 ? AlertSeverity.High :
///                riskScore >= 0.6 ? AlertSeverity.Medium :
///                AlertSeverity.Low;
/// </code>
/// </example>
public enum AlertSeverity
{
    /// <summary>
    /// Low severity - routine information or minor issues.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium severity - notable events that may require attention.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High severity - significant issues requiring prompt response.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical severity - urgent issues requiring immediate action.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Defines types of detections that can be identified by drone sensors and AI systems.
/// </summary>
/// <remarks>
/// These detection types represent the various objects, phenomena, and situations
/// that the ResQ AI detection system can identify from drone sensor data including
/// visual, thermal, and multi-spectral imagery.
/// </remarks>
/// <example>
/// <code>
/// // Check detection type
/// if (detection.Type == DetectionType.Person)
/// {
///     await DispatchRescueTeam(detection.Location);
/// }
/// else if (detection.Type == DetectionType.Fire)
/// {
///     await AlertFireDepartment(detection.Location);
/// }
/// </code>
/// </example>
public enum DetectionType
{
    /// <summary>
    /// Unknown or unclassified detection.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Human person detected.
    /// </summary>
    Person = 1,

    /// <summary>
    /// Vehicle (car, truck, boat, etc.) detected.
    /// </summary>
    Vehicle = 2,

    /// <summary>
    /// Fire or flames detected.
    /// </summary>
    Fire = 3,

    /// <summary>
    /// Flood water or flooding detected.
    /// </summary>
    Flood = 4,

    /// <summary>
    /// Debris or rubble detected.
    /// </summary>
    Debris = 5,

    /// <summary>
    /// Structural damage to buildings or infrastructure.
    /// </summary>
    StructuralDamage = 6,

    /// <summary>
    /// Survivor detected (person in need of rescue).
    /// </summary>
    Survivor = 7
}

/// <summary>
/// Defines mission types for drone operations.
/// </summary>
/// <remarks>
/// These mission types categorize the various operations that drones can perform
/// in the ResQ system. Each mission type has specific objectives, flight patterns,
/// and sensor requirements.
/// </remarks>
/// <example>
/// <code>
/// // Assign mission type
/// var mission = new Mission
/// {
///     Type = MissionType.Search,
///     TargetArea = disasterZone,
///     Priority = AlertSeverity.High
/// };
/// 
/// // Route based on mission type
/// switch (mission.Type)
/// {
///     case MissionType.Survey:
///         return CreateSurveyPattern(mission.TargetArea);
///     case MissionType.Search:
///         return CreateSearchPattern(mission.TargetArea);
///     case MissionType.Rescue:
///         return CreateRescueRoute(mission.TargetArea);
/// }
/// </code>
/// </example>
public enum MissionType
{
    /// <summary>
    /// Survey mission - mapping and general area assessment.
    /// </summary>
    Survey = 0,

    /// <summary>
    /// Delivery mission - transporting supplies or equipment.
    /// </summary>
    Delivery = 1,

    /// <summary>
    /// Search mission - looking for persons or objects.
    /// </summary>
    Search = 2,

    /// <summary>
    /// Rescue mission - active rescue operations.
    /// </summary>
    Rescue = 3,

    /// <summary>
    /// Assessment mission - damage and situation evaluation.
    /// </summary>
    Assessment = 4,

    /// <summary>
    /// Return to base mission - autonomous return to home.
    /// </summary>
    ReturnToBase = 5
}
