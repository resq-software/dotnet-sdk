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
using ResQ.Protocols;
using Xunit;

namespace ResQ.Protocols.Tests;

/// <summary>
/// Unit tests for Protocol extension methods.
/// </summary>
public class ProtocolExtensionsTests
{
    [Fact]
    public void NowUnixMs_ShouldReturnPositiveValue()
    {
        // Act
        var timestamp = ProtocolExtensions.NowUnixMs();

        // Assert
        timestamp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ToUnixMs_WithUtcNow_ShouldReturnRecentTimestamp()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var timestamp = now.ToUnixMs();

        // Assert
        timestamp.Should().BeGreaterThan(1700000000000); // After 2023
        timestamp.Should().BeLessThan(2000000000000); // Before 2033
    }

    [Fact]
    public void FromUnixMs_WithValidTimestamp_ShouldReturnCorrectDateTime()
    {
        // Arrange
        var timestamp = 1706800000000L; // 2024-02-01 12:00:00 UTC

        // Act
        var dateTime = timestamp.FromUnixMs();

        // Assert
        dateTime.Year.Should().Be(2024);
        dateTime.Month.Should().Be(2);
        dateTime.Day.Should().Be(1);
    }

    [Fact]
    public void ToUnixMs_ThenFromUnixMs_ShouldRoundTrip()
    {
        // Arrange
        var original = new DateTimeOffset(2024, 2, 12, 12, 0, 0, TimeSpan.Zero);

        // Act
        var timestamp = original.ToUnixMs();
        var roundTripped = timestamp.FromUnixMs();

        // Assert
        roundTripped.Should().BeCloseTo(original, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void FromUnixMs_WithZero_ShouldReturnUnixEpoch()
    {
        // Arrange
        var timestamp = 0L;

        // Act
        var dateTime = timestamp.FromUnixMs();

        // Assert
        dateTime.Year.Should().Be(1970);
        dateTime.Month.Should().Be(1);
        dateTime.Day.Should().Be(1);
    }

    [Theory]
    [InlineData(1000000000000L)] // 2001-09-09
    [InlineData(1500000000000L)] // 2017-07-14
    [InlineData(1700000000000L)] // 2023-11-15
    public void FromUnixMs_WithVariousTimestamps_ShouldReturnValidDates(long timestamp)
    {
        // Act
        var dateTime = timestamp.FromUnixMs();

        // Assert
        dateTime.Year.Should().BeGreaterThan(1970);
        dateTime.Year.Should().BeLessThan(2100);
    }

    [Fact]
    public void NowUnixMs_CalledTwice_ShouldReturnIncreasingValues()
    {
        // Act
        var first = ProtocolExtensions.NowUnixMs();
        Thread.Sleep(10); // Small delay
        var second = ProtocolExtensions.NowUnixMs();

        // Assert
        second.Should().BeGreaterThan(first);
    }
}
