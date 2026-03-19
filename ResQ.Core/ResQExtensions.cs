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
/// Provides extension methods for ResQ domain types.
/// </summary>
/// <remarks>
/// Contains utility extension methods that simplify working with ResQ types,
/// including severity conversion and critical detection checks.
/// </remarks>
/// <example>
/// <code>
/// // Convert risk score to severity
/// double riskScore = 0.85;
/// var severity = riskScore.ToSeverity(); // AlertSeverity.High
///
/// // Check if detection is critical
/// if (detection.IsCritical())
/// {
///     await TriggerImmediateResponse(detection);
/// }
/// </code>
/// </example>
public static class ResQExtensions
{
    /// <summary>
    /// Converts a risk score to an alert severity level.
    /// </summary>
    /// <param name="riskScore">The risk score (0.0 to 1.0).</param>
    /// <returns>The corresponding alert severity.</returns>
    /// <remarks>
    /// Risk score thresholds:
    /// <list type="bullet">
    /// <item><description>Critical: >= 0.9</description></item>
    /// <item><description>High: >= 0.75</description></item>
    /// <item><description>Medium: >= 0.6</description></item>
    /// <item><description>Low: &lt; 0.6</description></item>
    /// </list>
    /// </remarks>
    public static AlertSeverity ToSeverity(this double riskScore) => riskScore switch
    {
        >= 0.9 => AlertSeverity.Critical,
        >= 0.75 => AlertSeverity.High,
        >= 0.6 => AlertSeverity.Medium,
        _ => AlertSeverity.Low
    };

    /// <summary>
    /// Determines if a detection is critical and requires immediate action.
    /// </summary>
    /// <param name="detection">The detection to evaluate.</param>
    /// <returns>True if the detection is critical.</returns>
    /// <remarks>
    /// A detection is considered critical if it has high confidence (>= 0.85)
    /// and is of a critical type (Fire, Person, or Flood).
    /// </remarks>
    public static bool IsCritical(this Detection detection)
    {
        ArgumentNullException.ThrowIfNull(detection, nameof(detection));
        return detection.Confidence >= 0.85f &&
               detection.Type is DetectionType.Fire or DetectionType.Person or DetectionType.Flood;
    }
}
