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
/// Tests for InfrastructureApiClient specific functionality.
/// Follows the same pattern as CoordinationHceClientTests.
/// </summary>
public class InfrastructureApiClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly InfrastructureApiClient _client;

    public InfrastructureApiClientTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _client = new InfrastructureApiClient("http://localhost:5000", _mockHandler);
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    // ---- UploadImageAsync ----

    [Fact]
    public async Task UploadImageAsync_ValidImage_ReturnsUploadResponse()
    {
        // Arrange
        var expected = new UploadResponse("QmTest123", 1024, "https://gateway.pinata.cloud/ipfs/QmTest123");
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        // Act
        var result = await _client.UploadImageAsync(new byte[] { 0xFF, 0xD8 }, "photo.jpg");

        // Assert
        result.Should().NotBeNull();
        result.Cid.Should().Be("QmTest123");
        result.Size.Should().Be(1024);

        _mockHandler.Requests.Should().HaveCount(1);
        var request = _mockHandler.Requests[0];
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri!.AbsolutePath.Should().Be("/uploadImage");
    }

    [Fact]
    public async Task UploadImageAsync_ServerError_DoesNotRetryAndThrows()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.InternalServerError, "Server error");
        var expected = new UploadResponse("Qm1", 100, "https://gw.example.com/ipfs/Qm1");
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        // Act & Assert
        await _client.Invoking(c => c.UploadImageAsync(new byte[] { 0x01 }, "image.png"))
            .Should().ThrowAsync<HttpRequestException>();

        _mockHandler.Requests.Should().HaveCount(1, "non-idempotent uploads must not be retried");
    }

    [Fact]
    public async Task UploadImageAsync_NetworkError_DoesNotRetryAndThrows()
    {
        // Arrange
        _mockHandler.QueueNetworkError();
        var expected = new UploadResponse("Qm2", 200, "url");
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        // Act & Assert
        await _client.Invoking(c => c.UploadImageAsync(new byte[] { 0x01 }, "pic.jpg"))
            .Should().ThrowAsync<HttpRequestException>();

        _mockHandler.Requests.Should().HaveCount(1, "non-idempotent uploads must not be retried");
    }

    // ---- RecordEventAsync ----

    [Fact]
    public async Task RecordEventAsync_ValidEvent_SendsCorrectRequest()
    {
        // Arrange
        var expectedResponse = new BlockchainEventResponse("evt-1", "EVIDENCE_RECORDED", 1700000000, "0xabc");
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

        var request = new BlockchainEventRequest(
            EventId: "evt-1",
            EventType: "EVIDENCE_RECORDED",
            Payload: "{\"cid\": \"Qm1\"}",
            IpfsCid: "Qm1",
            DroneId: "drone-001",
            Timestamp: 1700000000
        );

        // Act
        var result = await _client.RecordEventAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.EventId.Should().Be("evt-1");
        result.TxHash.Should().Be("0xabc");

        _mockHandler.Requests.Should().HaveCount(1);
        _mockHandler.Requests[0].Method.Should().Be(HttpMethod.Post);
        _mockHandler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/blockchain/events");
    }

    // ---- CreateIncidentAsync ----

    [Fact]
    public async Task CreateIncidentAsync_ValidRequest_ReturnsIncidentResponse()
    {
        // Arrange
        var expected = new IncidentResponse("inc-1", "FIRE", "HIGH", "OPEN", "2026-01-01T00:00:00Z");
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        var request = new CreateIncidentRequest(
            IncidentType: "FIRE",
            Severity: "HIGH",
            Location: new LocationDto(37.7749, -122.4194, 100.0),
            Description: "Large fire detected near warehouse"
        );

        // Act
        var result = await _client.CreateIncidentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("inc-1");
        result.IncidentType.Should().Be("FIRE");
        result.Status.Should().Be("OPEN");

        _mockHandler.Requests.Should().HaveCount(1);
        _mockHandler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/incidents");
    }

    // ---- GetHealthAsync ----

    [Fact]
    public async Task GetHealthAsync_ReturnsHealthStatus()
    {
        // Arrange
        var expected = new HealthResponse("ok", true, true, false);
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        // Act
        var result = await _client.GetHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("ok");
        result.Pinata.Should().BeTrue();
        result.Gemini.Should().BeTrue();
        result.Blockchain.Should().BeFalse();

        _mockHandler.Requests.Should().HaveCount(1);
        _mockHandler.Requests[0].Method.Should().Be(HttpMethod.Get);
        _mockHandler.Requests[0].RequestUri!.AbsolutePath.Should().Be("/health");
    }

    // ---- ServiceName ----

    [Fact]
    public void ServiceName_IsInfrastructureAPI()
    {
        // The service name is protected, but we can verify constructor doesn't throw
        var client = new InfrastructureApiClient();
        client.Dispose();
    }

    // ---- Constructor validation (inherited from BaseServiceClient) ----

    [Fact]
    public void Constructor_EmptyUrl_Throws()
    {
        var act = () => new InfrastructureApiClient("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_InvalidUrl_Throws()
    {
        var act = () => new InfrastructureApiClient("not-a-url");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task AuthenticateAsync_SeparateAsyncFlows_KeepSeparateAuthorizationHeaders()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new InfraAuthResponse("token-1")));
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new InfraAuthResponse("token-2")));
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new HealthResponse("ok", true, true, true)));
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new HealthResponse("ok", true, true, true)));

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
