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
/// Lightweight environment update produced by translating a <see cref="Messages.ResqHazardZone"/> MAVLink message.
/// This record serves as the dialect-layer DTO; upstream services map this to their domain model.
/// </summary>
public sealed record EnvironmentUpdate
{
    /// <summary>Unix timestamp in milliseconds.</summary>
    public ulong TimestampMs { get; init; }

    /// <summary>Unique zone identifier.</summary>
    public uint ZoneId { get; init; }

    /// <summary>Zone centre latitude in degrees.</summary>
    public double CenterLatitudeDeg { get; init; }

    /// <summary>Zone centre longitude in degrees.</summary>
    public double CenterLongitudeDeg { get; init; }

    /// <summary>Zone radius in metres.</summary>
    public uint RadiusMetres { get; init; }

    /// <summary>Human-readable hazard type label.</summary>
    public string HazardType { get; init; } = string.Empty;

    /// <summary>Severity label (Low / Medium / High / Extreme).</summary>
    public string Severity { get; init; } = string.Empty;

    /// <summary>Expansion speed in m/s.</summary>
    public float ProgressionSpeedMs { get; init; }

    /// <summary>Expansion heading in radians.</summary>
    public float ProgressionHeadingRad { get; init; }
}
