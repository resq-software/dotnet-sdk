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

namespace ResQ.Mavlink.Dialect.Translation;

/// <summary>
/// Lightweight incident report produced by translating a <see cref="Messages.ResqDetection"/> MAVLink message.
/// This record serves as the dialect-layer DTO; upstream services map this to their domain model.
/// </summary>
public sealed record IncidentReport
{
    /// <summary>Unix timestamp in milliseconds.</summary>
    public ulong TimestampMs { get; init; }

    /// <summary>Incident latitude in degrees (full precision).</summary>
    public double LatitudeDeg { get; init; }

    /// <summary>Incident longitude in degrees (full precision).</summary>
    public double LongitudeDeg { get; init; }

    /// <summary>Altitude in metres above sea level.</summary>
    public double AltitudeMetres { get; init; }

    /// <summary>Human-readable detection type label.</summary>
    public string DetectionType { get; init; } = string.Empty;

    /// <summary>Detection confidence percent (0–100).</summary>
    public byte Confidence { get; init; }

    /// <summary>Bounding box as [x, y, width, height] in pixels.</summary>
    public int[] BoundingBox { get; init; } = Array.Empty<int>();
}
