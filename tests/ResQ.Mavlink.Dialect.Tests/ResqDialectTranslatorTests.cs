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

using FluentAssertions;
using ResQ.Mavlink.Dialect.Enums;
using ResQ.Mavlink.Dialect.Messages;
using ResQ.Mavlink.Dialect.Translation;
using Xunit;

namespace ResQ.Mavlink.Dialect.Tests;

/// <summary>
/// Tests for <see cref="ResqDialectTranslator"/> mapping dialect messages to domain DTOs.
/// </summary>
public sealed class ResqDialectTranslatorTests
{
    // ── MapDetectionToIncident ───────────────────────────────────────────────

    [Fact]
    public void MapDetectionToIncident_AllFields_MappedCorrectly()
    {
        var detection = new ResqDetection
        {
            TimestampMs = 1_711_400_000_000UL,
            LatE7 = 376_874_200,
            LonE7 = -1_222_313_100,
            AltMm = 150_000,
            BboxX = 10,
            BboxY = 20,
            BboxW = 100,
            BboxH = 80,
            DetectionType = ResqDetectionType.Person,
            Confidence = 92,
        };

        var report = ResqDialectTranslator.MapDetectionToIncident(detection);

        report.TimestampMs.Should().Be(1_711_400_000_000UL);
        report.LatitudeDeg.Should().BeApproximately(37.68742, 1e-4);
        report.LongitudeDeg.Should().BeApproximately(-122.23131, 1e-4);
        report.AltitudeMetres.Should().BeApproximately(150.0, 1e-6);
        report.DetectionType.Should().Be("Person");
        report.Confidence.Should().Be(92);
        report.BoundingBox.Should().Equal(10, 20, 100, 80);
    }

    [Theory]
    [InlineData(ResqDetectionType.Unknown, "Unknown")]
    [InlineData(ResqDetectionType.Person, "Person")]
    [InlineData(ResqDetectionType.Vehicle, "Vehicle")]
    [InlineData(ResqDetectionType.Fire, "Fire")]
    [InlineData(ResqDetectionType.Flood, "Flood")]
    [InlineData(ResqDetectionType.Debris, "Debris")]
    public void MapDetectionToIncident_DetectionTypeLabel_IsCorrect(ResqDetectionType type, string expectedLabel)
    {
        var detection = new ResqDetection { DetectionType = type };
        var report = ResqDialectTranslator.MapDetectionToIncident(detection);
        report.DetectionType.Should().Be(expectedLabel);
    }

    // ── MapHazardZoneToEnvironmentUpdate ────────────────────────────────────

    [Fact]
    public void MapHazardZoneToEnvironmentUpdate_AllFields_MappedCorrectly()
    {
        var zone = new ResqHazardZone
        {
            TimestampMs = 1_711_500_000_000UL,
            ZoneId = 42u,
            CenterLatE7 = 377_000_000,
            CenterLonE7 = -1_221_000_000,
            RadiusMetres = 500u,
            ProgressionSpeed = 2.5f,
            ProgressionHeading = 1.2f,
            HazardType = ResqHazardType.Fire,
            Severity = (ResqHazardSeverity)3,
        };

        var update = ResqDialectTranslator.MapHazardZoneToEnvironmentUpdate(zone);

        update.TimestampMs.Should().Be(1_711_500_000_000UL);
        update.ZoneId.Should().Be(42u);
        update.CenterLatitudeDeg.Should().BeApproximately(37.7, 1e-4);
        update.CenterLongitudeDeg.Should().BeApproximately(-122.1, 1e-4);
        update.RadiusMetres.Should().Be(500u);
        update.HazardType.Should().Be("Fire");
        update.Severity.Should().Be("Extreme");
        update.ProgressionSpeedMs.Should().BeApproximately(2.5f, 1e-5f);
        update.ProgressionHeadingRad.Should().BeApproximately(1.2f, 1e-5f);
    }

    [Theory]
    [InlineData(ResqHazardType.Fire, "Fire")]
    [InlineData(ResqHazardType.Flood, "Flood")]
    [InlineData(ResqHazardType.Wind, "Wind")]
    [InlineData(ResqHazardType.Toxic, "Toxic")]
    [InlineData(ResqHazardType.Structural, "Structural")]
    public void MapHazardZoneToEnvironmentUpdate_HazardTypeLabel_IsCorrect(ResqHazardType type, string expectedLabel)
    {
        var zone = new ResqHazardZone { HazardType = type };
        var update = ResqDialectTranslator.MapHazardZoneToEnvironmentUpdate(zone);
        update.HazardType.Should().Be(expectedLabel);
    }

    [Theory]
    [InlineData(0, "Low")]
    [InlineData(1, "Medium")]
    [InlineData(2, "High")]
    [InlineData(3, "Extreme")]
    public void MapHazardZoneToEnvironmentUpdate_SeverityLabel_IsCorrect(byte severity, string expectedLabel)
    {
        var zone = new ResqHazardZone { Severity = (ResqHazardSeverity)severity };
        var update = ResqDialectTranslator.MapHazardZoneToEnvironmentUpdate(zone);
        update.Severity.Should().Be(expectedLabel);
    }
}
