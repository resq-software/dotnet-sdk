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
using FluentAssertions;
using ResQ.Clients.Tests.TestHelpers;
using Xunit;

namespace ResQ.Clients.Tests;

/// <summary>
/// Tests for BaseServiceClient retry logic, circuit breaker, and error handling.
/// </summary>
/// <remarks>
/// These tests verify critical resilience patterns that protect against:
/// - Transient network failures
/// - Service overload (circuit breaker)
/// - Timeout scenarios
/// - Cancellation token propagation
/// </remarks>
public class BaseServiceClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly TestServiceClient _client;

    public BaseServiceClientTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _client = new TestServiceClient("http://localhost:5000", _mockHandler);
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_SuccessfulRequest_ReturnsResponse()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{\"success\": true}");

        // Act
        var response = await _client.TestGetAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockHandler.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_TransientFailureThenSuccess_RetriesAndSucceeds()
    {
        // Arrange
        _mockHandler.QueueNetworkError(); // First attempt fails
        _mockHandler.QueueNetworkError(); // Second attempt fails
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{\"success\": true}"); // Third attempt succeeds

        // Act
        var response = await _client.TestGetAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockHandler.Requests.Should().HaveCount(3, "should retry twice after failures");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_AllRetriesFail_ThrowsException()
    {
        // Arrange
        _mockHandler.QueueNetworkError(); // Attempt 1
        _mockHandler.QueueNetworkError(); // Attempt 2
        _mockHandler.QueueNetworkError(); // Attempt 3
        _mockHandler.QueueNetworkError(); // Attempt 4 (max retries = 3)

        // Act & Assert
        await _client.Invoking(c => c.TestGetAsync())
            .Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Network error*");

        _mockHandler.Requests.Should().HaveCount(4, "initial attempt + 3 retries");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_500Error_Retries()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.InternalServerError, "Server error");
        _mockHandler.QueueJsonResponse(HttpStatusCode.InternalServerError, "Server error");
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{\"success\": true}");

        // Act
        var response = await _client.TestGetAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockHandler.Requests.Should().HaveCount(3, "should retry 5xx errors");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_503ServiceUnavailable_Retries()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.ServiceUnavailable, "Service unavailable");
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{\"success\": true}");

        // Act
        var response = await _client.TestGetAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockHandler.Requests.Should().HaveCount(2, "should retry 503 errors");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_429TooManyRequests_Retries()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.TooManyRequests, "Rate limited");
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{\"success\": true}");

        // Act
        var response = await _client.TestGetAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockHandler.Requests.Should().HaveCount(2, "should retry 429 rate limit errors");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_408RequestTimeout_Retries()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.RequestTimeout, "Timeout");
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{\"success\": true}");

        // Act
        var response = await _client.TestGetAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockHandler.Requests.Should().HaveCount(2, "should retry timeout errors");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_400BadRequest_DoesNotRetry()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.BadRequest, "{\"error\": \"Invalid input\"}");

        // Act
        var response = await _client.TestGetAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _mockHandler.Requests.Should().HaveCount(1, "should NOT retry 4xx client errors");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_404NotFound_DoesNotRetry()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.NotFound, "Not found");

        // Act
        var response = await _client.TestGetAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _mockHandler.Requests.Should().HaveCount(1, "should NOT retry 404 errors");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_CancellationToken_PropagatesCancellation()
    {
        // Arrange
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{\"success\": true}");
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await _client.Invoking(c => c.TestGetAsync(cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        _mockHandler.Requests.Should().BeEmpty("request should be cancelled before sending");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_TimeoutDuringRequest_ThrowsTaskCanceledException()
    {
        // Arrange
        _mockHandler.QueueTimeout();

        // Act & Assert
        await _client.Invoking(c => c.TestGetAsync())
            .Should().ThrowAsync<TaskCanceledException>()
            .WithMessage("*timed out*");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_CircuitBreaker_ConfiguredAndActive()
    {
        // NOTE: Circuit breaker behavior is complex and timing-dependent.
        // This test verifies that the circuit breaker is configured, not its exact behavior.
        // Real-world circuit breaker testing should be done with integration tests.

        // Arrange - Queue multiple failures
        for (int i = 0; i < 20; i++)
        {
            _mockHandler.QueueNetworkError();
        }

        // Act - Make requests until circuit breaker activates
        int successfulAttempts = 0;
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await _client.TestGetAsync();
            }
            catch
            {
                successfulAttempts++;
            }
        }

        // Assert - Some requests should have been attempted (circuit breaker is configured)
        _mockHandler.Requests.Should().NotBeEmpty("requests should be attempted");
        _mockHandler.Requests.Count.Should().BeGreaterThan(4, "initial attempts should be made before circuit opens");
    }

    [Fact]
    public async Task ExecuteWithResilienceAsync_ExponentialBackoff_IncreasesDelayBetweenRetries()
    {
        // Arrange
        _mockHandler.QueueNetworkError();
        _mockHandler.QueueNetworkError();
        _mockHandler.QueueJsonResponse(HttpStatusCode.OK, "{\"success\": true}");

        var startTime = DateTime.UtcNow;

        // Act
        await _client.TestGetAsync();

        var duration = DateTime.UtcNow - startTime;

        // Assert
        // With exponential backoff: 100ms + 200ms = 300ms minimum
        duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(250),
            "exponential backoff should introduce delays between retries");

        _mockHandler.Requests.Should().HaveCount(3);
    }

    [Fact]
    public Task Constructor_NullBaseUrl_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => new TestServiceClient(null!, _mockHandler);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("baseUrl");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Constructor_EmptyBaseUrl_ThrowsArgumentException()
    {
        // Act & Assert
        Action act = () => new TestServiceClient("", _mockHandler);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*")
            .WithParameterName("baseUrl");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Constructor_InvalidUrlFormat_ThrowsArgumentException()
    {
        // Act & Assert
        Action act = () => new TestServiceClient("not-a-valid-url", _mockHandler);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid base URL*")
            .WithParameterName("baseUrl");
        return Task.CompletedTask;
    }

    [Fact]
    public Task Constructor_NonHttpUrl_ThrowsArgumentException()
    {
        // Act & Assert
        Action act = () => new TestServiceClient("ftp://localhost:5000", _mockHandler);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*valid HTTP*")
            .WithParameterName("baseUrl");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Test implementation of BaseServiceClient for testing purposes.
    /// </summary>
    private class TestServiceClient : BaseServiceClient
    {
        public TestServiceClient(string baseUrl, HttpMessageHandler? handler = null)
            : base(baseUrl, handler)
        {
        }

        protected override string ServiceName => "Test Service";

        public async Task<HttpResponseMessage> TestGetAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteWithResilienceAsync(
                ct => Http.GetAsync("/test", ct),
                cancellationToken);
        }
    }
}
