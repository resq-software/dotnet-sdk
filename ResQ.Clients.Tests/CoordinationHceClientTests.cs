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

using System.Net;
using System.Text.Json;
using FluentAssertions;
using ResQ.Clients.Tests.TestHelpers;
using Xunit;

namespace ResQ.Clients.Tests;

/// <summary>
/// Tests for CoordinationHceClient specific functionality.
/// </summary>
public class CoordinationHceClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly CoordinationHceClient _client;

    public CoordinationHceClientTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _client = new CoordinationHceClient("http://localhost:5000", _mockHandler);
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SendTelemetryBatchAsync_ValidBatch_SendsCorrectRequest()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{}");

        var batch = new TelemetryBatchRequest(
            DroneId: "drone-001",
            Packets: new List<TelemetryPacket>
            {
                new(
                    DroneId: "drone-001",
                    Latitude: 37.7749,
                    Longitude: -122.4194,
                    Altitude: 100.0,
                    Battery: 85.5,
                    FlightMode: "ARMED",
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                )
            }
        );

        // Act
        await _client.SendTelemetryBatchAsync(batch);

        // Assert
        _mockHandler.Requests.Should().HaveCount(1);
        var request = _mockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri!.AbsolutePath.Should().Be("/v1/telemetry/batch");
    }

    [Fact]
    public async Task SendTelemetryBatchAsync_NetworkError_DoesNotRetryAndThrows()
    {
        // Arrange
        _mockHandler.QueueNetworkError();
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{}");

        var batch = new TelemetryBatchRequest(
            DroneId: "drone-001",
            Packets: new List<TelemetryPacket>
            {
                new("drone-001", 37.7749, -122.4194, 100.0, 85.5, "ARMED", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            }
        );

        // Act & Assert
        await _client.Invoking(c => c.SendTelemetryBatchAsync(batch))
            .Should().ThrowAsync<HttpRequestException>();

        _mockHandler.Requests.Should().HaveCount(1, "non-idempotent telemetry posts must not be retried");
    }

    [Fact]
    public async Task ReportIncidentAsync_ValidIncident_SendsCorrectRequest()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{\"incidentId\": \"inc-001\", \"status\": \"REPORTED\"}");

        var incident = new ReportIncidentRequest(
            IncidentType: "FIRE",
            Severity: "HIGH",
            Location: new LocationDto(37.7749, -122.4194, 100.0),
            Description: "Large fire detected"
        );

        // Act
        var result = await _client.ReportIncidentAsync(incident);

        // Assert
        result.Should().NotBeNull();
        result.IncidentId.Should().Be("inc-001");

        _mockHandler.Requests.Should().HaveCount(1);
        var request = _mockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri!.AbsolutePath.Should().Be("/v1/incident");
    }

    [Fact]
    public async Task ReportIncidentAsync_500Error_DoesNotRetryAndThrows()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.InternalServerError, "Server error");
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{\"incidentId\": \"inc-001\", \"status\": \"REPORTED\"}");

        var incident = new ReportIncidentRequest(
            IncidentType: "FIRE",
            Severity: "HIGH",
            Location: new LocationDto(37.7749, -122.4194, 100.0),
            Description: "Fire detected"
        );

        // Act & Assert
        await _client.Invoking(c => c.ReportIncidentAsync(incident))
            .Should().ThrowAsync<HttpRequestException>();

        _mockHandler.Requests.Should().HaveCount(1, "non-idempotent incident reports must not be retried");
    }

    [Fact]
    public async Task GetFleetStatusAsync_ValidFleetId_ReturnsStatus()
    {
        // Arrange
        var fleetStatus = new FleetStatus(
            FleetId: "fleet-01",
            ActiveDrones: 5,
            TotalMissions: 10
        );

        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(fleetStatus));

        // Act
        var result = await _client.GetFleetStatusAsync("fleet-01");

        // Assert
        result.Should().NotBeNull();
        result.FleetId.Should().Be("fleet-01");
        result.ActiveDrones.Should().Be(5);

        _mockHandler.Requests.Should().HaveCount(1);
        var request = _mockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.AbsolutePath.Should().Be("/fleet/fleet-01");
    }

    [Fact]
    public async Task SendTelemetryBatchAsync_EmptyPackets_ThrowsArgumentException()
    {
        // Arrange
        var batch = new TelemetryBatchRequest(
            DroneId: "drone-001",
            Packets: new List<TelemetryPacket>() // Empty
        );

        // Act & Assert
        await _client.Invoking(c => c.SendTelemetryBatchAsync(batch))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public async Task SendTelemetryBatchAsync_NullDroneId_ThrowsArgumentException()
    {
        // Arrange
        var batch = new TelemetryBatchRequest(
            DroneId: null!, // Null
            Packets: new List<TelemetryPacket>
            {
                new("drone-001", 37.7749, -122.4194, 100.0, 85.5, "ARMED", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            }
        );

        // Act & Assert
        await _client.Invoking(c => c.SendTelemetryBatchAsync(batch))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReportIncidentAsync_InvalidLatitude_ThrowsArgumentException()
    {
        // Arrange
        var incident = new ReportIncidentRequest(
            IncidentType: "FIRE",
            Severity: "HIGH",
            Location: new LocationDto(999.0, -122.4194, 100.0), // Invalid latitude
            Description: "Fire detected"
        );

        // Act & Assert
        await _client.Invoking(c => c.ReportIncidentAsync(incident))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Latitude*between -90 and 90*");
    }

    [Fact]
    public async Task ReportIncidentAsync_InvalidLongitude_ThrowsArgumentException()
    {
        // Arrange
        var incident = new ReportIncidentRequest(
            IncidentType: "FIRE",
            Severity: "HIGH",
            Location: new LocationDto(37.7749, -999.0, 100.0), // Invalid longitude
            Description: "Fire detected"
        );

        // Act & Assert
        await _client.Invoking(c => c.ReportIncidentAsync(incident))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Longitude*between -180 and 180*");
    }

    [Fact]
    public async Task AuthenticateAsync_SeparateAsyncFlows_KeepSeparateAuthorizationHeaders()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new AuthResponse("token-1")));
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new AuthResponse("token-2")));
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new HceHealthResponse("ok")));
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new HceHealthResponse("ok")));

        var firstAuthenticated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAuthenticated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var flowOne = Task.Run(async () =>
        {
            (await _client.AuthenticateAsync("user-1", "password-1")).Should().BeTrue();
            firstAuthenticated.SetResult();
            await secondAuthenticated.Task;
            await _client.GetHealthAsync();
        });

        var flowTwo = Task.Run(async () =>
        {
            await firstAuthenticated.Task;
            (await _client.AuthenticateAsync("user-2", "password-2")).Should().BeTrue();
            secondAuthenticated.SetResult();
            await _client.GetHealthAsync();
        });

        await Task.WhenAll(flowOne, flowTwo);

        var healthRequests = _mockHandler.Requests
            .Where(r => r.RequestUri?.AbsolutePath == "/health")
            .ToList();

        healthRequests.Should().HaveCount(2);
        healthRequests[0].Headers.Authorization?.Parameter.Should().Be("token-1");
        healthRequests[1].Headers.Authorization?.Parameter.Should().Be("token-2");
    }
}
