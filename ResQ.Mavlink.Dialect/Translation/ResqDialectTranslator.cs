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

using ResQ.Mavlink.Dialect.Enums;
using ResQ.Mavlink.Dialect.Messages;

namespace ResQ.Mavlink.Dialect.Translation;

/// <summary>
/// Translates ResQ dialect MAVLink messages into domain DTOs suitable for upstream services.
/// </summary>
public static class ResqDialectTranslator
{
    private const double DegE7Factor = 1e-7;
    private const double MmToMetres = 1e-3;

    /// <summary>
    /// Maps a <see cref="ResqDetection"/> MAVLink message to an <see cref="IncidentReport"/>.
    /// </summary>
    /// <param name="det">The detection message received from the swarm.</param>
    /// <returns>A domain-level incident report.</returns>
    public static IncidentReport MapDetectionToIncident(ResqDetection det) => new()
    {
        TimestampMs = det.TimestampMs,
        LatitudeDeg = det.LatE7 * DegE7Factor,
        LongitudeDeg = det.LonE7 * DegE7Factor,
        AltitudeMetres = det.AltMm * MmToMetres,
        DetectionType = DetectionTypeLabel(det.DetectionType),
        Confidence = det.Confidence,
        BoundingBox = [det.BboxX, det.BboxY, det.BboxW, det.BboxH],
    };

    /// <summary>
    /// Maps a <see cref="ResqHazardZone"/> MAVLink message to an <see cref="EnvironmentUpdate"/>.
    /// </summary>
    /// <param name="zone">The hazard zone message received from the swarm.</param>
    /// <returns>A domain-level environment update.</returns>
    public static EnvironmentUpdate MapHazardZoneToEnvironmentUpdate(ResqHazardZone zone) => new()
    {
        TimestampMs = zone.TimestampMs,
        ZoneId = zone.ZoneId,
        CenterLatitudeDeg = zone.CenterLatE7 * DegE7Factor,
        CenterLongitudeDeg = zone.CenterLonE7 * DegE7Factor,
        RadiusMetres = zone.RadiusMetres,
        HazardType = HazardTypeLabel(zone.HazardType),
        Severity = SeverityLabel((byte)zone.Severity),
        ProgressionSpeedMs = zone.ProgressionSpeed,
        ProgressionHeadingRad = zone.ProgressionHeading,
    };

    // ── private helpers ────────────────────────────────────────────────────

    private static string DetectionTypeLabel(ResqDetectionType t) => t switch
    {
        ResqDetectionType.Person => "Person",
        ResqDetectionType.Vehicle => "Vehicle",
        ResqDetectionType.Fire => "Fire",
        ResqDetectionType.Flood => "Flood",
        ResqDetectionType.Debris => "Debris",
        _ => "Unknown",
    };

    private static string HazardTypeLabel(ResqHazardType t) => t switch
    {
        ResqHazardType.Fire => "Fire",
        ResqHazardType.Flood => "Flood",
        ResqHazardType.Wind => "Wind",
        ResqHazardType.Toxic => "Toxic",
        ResqHazardType.Structural => "Structural",
        _ => "Unknown",
    };

    private static string SeverityLabel(byte severity) => severity switch
    {
        0 => "Low",
        1 => "Medium",
        2 => "High",
        3 => "Extreme",
        _ => "Unknown",
    };
}
