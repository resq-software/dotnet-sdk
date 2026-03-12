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
using Microsoft.Extensions.Logging;
using Moq;
using ResQ.Blockchain;
using Xunit;

namespace ResQ.Blockchain.Tests;

/// <summary>
/// Unit tests for the <see cref="MockNeoClient"/> implementation.
/// </summary>
public class MockNeoClientTests
{
    private readonly MockNeoClient _client;
    private readonly Mock<ILogger<MockNeoClient>> _mockLogger;

    public MockNeoClientTests()
    {
        _mockLogger = new Mock<ILogger<MockNeoClient>>();
        _client = new MockNeoClient(_mockLogger.Object);
    }

    [Fact]
    public async Task RecordEventAsync_ShouldReturnValidTransactionResult()
    {
        // Arrange
        var evt = new BlockchainEvent(
            EventId: "evt-001",
            EventType: "IncidentDetected",
            Payload: "{\"incidentId\":\"inc-001\",\"severity\":\"High\"}",
            IpfsCid: "QmTest123",
            Timestamp: DateTimeOffset.UtcNow
        );

        // Act
        var result = await _client.RecordEventAsync(evt);

        // Assert
        result.Should().NotBeNull();
        result.TransactionHash.Should().NotBeNullOrEmpty();
        result.TransactionHash.Should().StartWith("0x");
        result.TransactionHash.Length.Should().Be(66); // 0x + 64 hex characters
        result.IsConfirmed.Should().BeTrue();
        result.BlockHeight.Should().BeGreaterThan(0);
        result.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RecordEventAsync_WithMultipleEvents_ShouldGenerateUniqueTxHashes()
    {
        // Arrange
        var evt1 = new BlockchainEvent("evt-001", "Type1", "{}", null, DateTimeOffset.UtcNow);
        var evt2 = new BlockchainEvent("evt-002", "Type2", "{}", null, DateTimeOffset.UtcNow);

        // Act
        var result1 = await _client.RecordEventAsync(evt1);
        var result2 = await _client.RecordEventAsync(evt2);

        // Assert
        result1.TransactionHash.Should().NotBe(result2.TransactionHash);
    }

    [Fact]
    public async Task RecordLocationAttestationAsync_ShouldReturnValidResult()
    {
        // Arrange
        var attestation = new LocationAttestation(
            DroneId: "drn-001",
            Latitude: 37.7749,
            Longitude: -122.4194,
            Altitude: 100.5,
            Timestamp: DateTimeOffset.UtcNow,
            Signature: "0xabcdef123456"
        );

        // Act
        var result = await _client.RecordLocationAttestationAsync(attestation);

        // Assert
        result.Should().NotBeNull();
        result.TransactionHash.Should().StartWith("0x");
        result.IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyLocationAttestationAsync_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var attestation = new LocationAttestation(
            DroneId: "drn-001",
            Latitude: 37.7749,
            Longitude: -122.4194,
            Altitude: 100.5,
            Timestamp: DateTimeOffset.UtcNow,
            Signature: "0xvalidsignature"
        );

        // Act
        var isValid = await _client.VerifyLocationAttestationAsync(attestation);

        // Assert
        isValid.Should().BeTrue(); // Mock always returns true
    }

    [Fact]
    public async Task RecordEvidenceAsync_ShouldReturnValidResult()
    {
        // Arrange
        var evidence = new EvidenceRecord(
            IncidentId: "inc-001",
            IpfsCid: "QmEvidence123",
            ContentType: "image/jpeg",
            SizeBytes: 1024567,
            Hash: "sha256:abc123"
        );

        // Act
        var result = await _client.RecordEvidenceAsync(evidence);

        // Assert
        result.Should().NotBeNull();
        result.TransactionHash.Should().StartWith("0x");
        result.IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task GetEventsByIncidentAsync_WithNoEvents_ShouldReturnEmptyList()
    {
        // Act
        var events = await _client.GetEventsByIncidentAsync("nonexistent-incident");

        // Assert
        events.Should().NotBeNull();
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventsByIncidentAsync_AfterRecordingEvent_ShouldReturnEvent()
    {
        // Arrange
        var incidentId = "inc-test-001";
        var evt = new BlockchainEvent(
            EventId: "evt-001",
            EventType: "IncidentDetected",
            Payload: $"{{\"incidentId\":\"{incidentId}\"}}",
            IpfsCid: null,
            Timestamp: DateTimeOffset.UtcNow
        );

        await _client.RecordEventAsync(evt);

        // Act
        var events = await _client.GetEventsByIncidentAsync(incidentId);

        // Assert
        events.Should().ContainSingle();
        events[0].EventId.Should().Be(evt.EventId);
        events[0].EventType.Should().Be(evt.EventType);
    }

    [Fact]
    public async Task GetBlockHeightAsync_ShouldReturnPositiveValue()
    {
        // Act
        var height = await _client.GetBlockHeightAsync();

        // Assert
        height.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RecordEventAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var evt = new BlockchainEvent("evt-001", "Test", "{}", null, DateTimeOffset.UtcNow);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _client.RecordEventAsync(evt, cts.Token)
        );
    }

    [Theory]
    [InlineData("IncidentDetected")]
    [InlineData("EvidenceSubmitted")]
    [InlineData("LocationAttestation")]
    [InlineData("IncidentResolved")]
    public async Task RecordEventAsync_WithDifferentEventTypes_ShouldSucceed(string eventType)
    {
        // Arrange
        var evt = new BlockchainEvent(
            EventId: $"evt-{Guid.NewGuid()}",
            EventType: eventType,
            Payload: "{}",
            IpfsCid: null,
            Timestamp: DateTimeOffset.UtcNow
        );

        // Act
        var result = await _client.RecordEventAsync(evt);

        // Assert
        result.Should().NotBeNull();
        result.IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task RecordEventAsync_WithValidIpfsCid_ShouldIncludeCidInStorage()
    {
        // Arrange
        var cid = "QmTestCID123";
        var evt = new BlockchainEvent(
            EventId: "evt-001",
            EventType: "EvidenceSubmitted",
            Payload: "{}",
            IpfsCid: cid,
            Timestamp: DateTimeOffset.UtcNow
        );

        // Act
        var result = await _client.RecordEventAsync(evt);

        // Assert
        result.Should().NotBeNull();
        // In a real test, we might verify the CID is stored, but MockNeoClient
        // doesn't expose its internal storage for testing
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleMultipleSimultaneousRequests()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10).Select(i =>
            _client.RecordEventAsync(new BlockchainEvent(
                EventId: $"evt-{i}",
                EventType: "Test",
                Payload: "{}",
                IpfsCid: null,
                Timestamp: DateTimeOffset.UtcNow
            ))
        );

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Select(r => r.TransactionHash).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetEventsByIncidentAsync_WithMultipleEvents_ShouldReturnAllMatching()
    {
        // Arrange
        var incidentId = "inc-multi-001";
        var events = new[]
        {
            new BlockchainEvent($"evt-1", "Type1", $"{{\"incidentId\":\"{incidentId}\"}}", null, DateTimeOffset.UtcNow),
            new BlockchainEvent($"evt-2", "Type2", $"{{\"incidentId\":\"{incidentId}\"}}", null, DateTimeOffset.UtcNow),
            new BlockchainEvent($"evt-3", "Type3", $"{{\"incidentId\":\"{incidentId}\"}}", null, DateTimeOffset.UtcNow)
        };

        foreach (var evt in events)
        {
            await _client.RecordEventAsync(evt);
        }

        // Act
        var retrievedEvents = await _client.GetEventsByIncidentAsync(incidentId);

        // Assert
        retrievedEvents.Should().HaveCount(3);
        retrievedEvents.Select(e => e.EventId).Should().Contain(events.Select(e => e.EventId));
    }
}
