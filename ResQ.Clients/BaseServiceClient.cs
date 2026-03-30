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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using System.Net.Http.Headers;

namespace ResQ.Clients;

/// <summary>
/// Abstract base class for ResQ service clients.
/// Provides common HTTP client setup with resilience patterns (retry, circuit breaker, timeout).
/// </summary>
public abstract class BaseServiceClient : IDisposable
{
    protected readonly HttpClient Http;
    protected readonly string BaseUrl;
    protected readonly ResiliencePipeline<HttpResponseMessage> RetryingPipeline;
    protected readonly ResiliencePipeline<HttpResponseMessage> NonRetryingPipeline;
    protected readonly ILogger Logger;
    private readonly AsyncLocal<AuthenticationHeaderValue?> _authorizationHeader = new();

    // Resilience configuration (can be overridden by derived classes)
    protected virtual int MaxRetries => 3;
    protected virtual int InitialRetryDelayMs => 100;
    protected virtual int CircuitBreakerFailureThreshold => 5;
    protected virtual int CircuitBreakerBreakDurationSec => 30;
    protected virtual int RequestTimeoutSec => 10;

    /// <summary>
    /// Service name for logging purposes (e.g., "Infrastructure API", "Coordination HCE").
    /// </summary>
    protected abstract string ServiceName { get; }

    protected BaseServiceClient(string baseUrl, HttpMessageHandler? handler = null, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(baseUrl, nameof(baseUrl));

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be empty", nameof(baseUrl));

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid base URL format", nameof(baseUrl));

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Base URL must be a valid HTTP or HTTPS URL", nameof(baseUrl));

        BaseUrl = baseUrl;
        Logger = logger ?? NullLogger.Instance;

        // Use provided handler for testing, or create default HttpClient
        Http = handler != null
            ? new HttpClient(handler) { BaseAddress = new Uri(baseUrl) }
            : new HttpClient { BaseAddress = new Uri(baseUrl) };

        RetryingPipeline = BuildResiliencePipeline(enableRetries: true);
        NonRetryingPipeline = BuildResiliencePipeline(enableRetries: false);
    }

    /// <summary>
    /// Gets or sets the authorization header for the current async flow.
    /// </summary>
    protected AuthenticationHeaderValue? AuthorizationHeader
    {
        get => _authorizationHeader.Value;
        set => _authorizationHeader.Value = value;
    }

    /// <summary>
    /// Builds the resilience pipeline with circuit breaker, timeout, and optional retries.
    /// </summary>
    private ResiliencePipeline<HttpResponseMessage> BuildResiliencePipeline(bool enableRetries)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        if (enableRetries)
        {
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = MaxRetries,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(InitialRetryDelayMs),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(r => !r.IsSuccessStatusCode && (
                        (int)r.StatusCode >= 500 ||  // 5xx server errors
                        r.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||  // 429 rate limit
                        r.StatusCode == System.Net.HttpStatusCode.RequestTimeout  // 408 timeout
                    )),
                OnRetry = args =>
                {
                    Logger.LogWarning(
                        "[{ServiceName}] Retry {AttemptNumber}/{MaxRetries} after {RetryDelay}ms",
                        ServiceName, args.AttemptNumber, MaxRetries, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            });
        }

        return builder
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                MinimumThroughput = CircuitBreakerFailureThreshold,
                BreakDuration = TimeSpan.FromSeconds(CircuitBreakerBreakDurationSec),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnOpened = args =>
                {
                    Logger.LogError(
                        "[{ServiceName}] Circuit breaker OPENED - too many failures",
                        ServiceName);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    Logger.LogInformation(
                        "[{ServiceName}] Circuit breaker CLOSED - service recovered",
                        ServiceName);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(RequestTimeoutSec))
            .Build();
    }

    /// <summary>
    /// Executes an HTTP request with a resilience policy appropriate for the HTTP method.
    /// </summary>
    protected async Task<HttpResponseMessage> ExecuteWithResilienceAsync(
        HttpMethod method,
        Func<CancellationToken, Task<HttpResponseMessage>> action,
        CancellationToken cancellationToken = default)
    {
        var pipeline = IsIdempotent(method) ? RetryingPipeline : NonRetryingPipeline;

        return await pipeline.ExecuteAsync(
            async ct => await action(ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP request, applying authorization from the current async flow when present.
    /// </summary>
    protected async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string requestUri,
        HttpContent? content = null,
        CancellationToken cancellationToken = default,
        bool includeAuthorization = true)
    {
        return await ExecuteWithResilienceAsync(
            method,
            async ct =>
            {
                using var request = new HttpRequestMessage(method, requestUri)
                {
                    Content = content
                };

                if (includeAuthorization && AuthorizationHeader != null)
                {
                    request.Headers.Authorization = AuthorizationHeader;
                }

                return await Http.SendAsync(request, ct).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static bool IsIdempotent(HttpMethod method)
    {
        return method == HttpMethod.Get
            || method == HttpMethod.Head
            || method == HttpMethod.Options
            || method == HttpMethod.Trace;
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose()
    {
        Http?.Dispose();
        GC.SuppressFinalize(this);
    }
}
