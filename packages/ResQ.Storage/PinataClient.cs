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

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;

namespace ResQ.Storage;

/// <summary>
/// Pinata IPFS client implementation of <see cref="IStorageClient"/>.
/// </summary>
/// <remarks>
/// This client provides integration with the Pinata IPFS pinning service, allowing
/// files to be uploaded to IPFS and pinned for guaranteed availability. The client
/// supports both JWT token and API key/secret authentication methods.
///
/// <para>
/// When <see cref="PinataOptions.MockMode"/> is enabled, the client generates fake CIDs
/// using SHA256 hashes of the content without making actual API calls. This is useful
/// for testing and development without consuming Pinata credits.
/// </para>
///
/// <para>
/// The client is designed to be used with dependency injection and requires an
/// <see cref="HttpClient"/> configured with appropriate base address and authentication headers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Dependency injection registration
/// services.AddHttpClient&lt;IStorageClient, PinataClient&gt;();
/// services.Configure&lt;PinataOptions&gt;(options =>
/// {
///     options.JwtToken = Environment.GetEnvironmentVariable("PINATA_JWT");
///     options.MockMode = false;
/// });
///
/// // Usage
/// public class EvidenceService
/// {
///     private readonly IStorageClient _storage;
///
///     public EvidenceService(IStorageClient storage)
///     {
///         _storage = storage;
///     }
///
///     public async Task&lt;string&gt; UploadEvidenceAsync(byte[] imageData)
///     {
///         var result = await _storage.UploadAsync(imageData, "evidence.jpg", "image/jpeg");
///         return result.Cid;
///     }
/// }
/// </code>
/// </example>
public class PinataClient : IStorageClient
{
    private readonly HttpClient _httpClient;
    private readonly PinataOptions _options;
    private readonly ILogger<PinataClient> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="PinataClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance for making API requests.</param>
    /// <param name="options">The Pinata configuration options.</param>
    /// <param name="logger">The logger instance for recording operations.</param>
    /// <remarks>
    /// The constructor configures the HTTP client with the base address, timeout,
    /// and authentication headers based on the provided options. JWT authentication
    /// is preferred over API key/secret when both are provided.
    /// </remarks>
    /// <example>
    /// <code>
    /// var httpClient = new HttpClient();
    /// var options = Options.Create(new PinataOptions
    /// {
    ///     JwtToken = "your-jwt-token",
    ///     ApiUrl = "https://api.pinata.cloud"
    /// });
    /// var logger = loggerFactory.CreateLogger&lt;PinataClient&gt;();
    ///
    /// var client = new PinataClient(httpClient, options, logger);
    /// </code>
    /// </example>
    public PinataClient(
        HttpClient httpClient,
        IOptions<PinataOptions> options,
        ILogger<PinataClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        ConfigureHttpClient();
        _resiliencePipeline = BuildResiliencePipeline();
    }

    /// <summary>
    /// Builds the resilience pipeline with retry and circuit breaker policies.
    /// </summary>
    private ResiliencePipeline<HttpResponseMessage> BuildResiliencePipeline()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(100),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => !r.IsSuccessStatusCode && (
                        (int)r.StatusCode >= 500 ||
                        r.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                        r.StatusCode == System.Net.HttpStatusCode.RequestTimeout
                    )),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Pinata upload retry {AttemptNumber}/3 after {RetryDelay}ms due to {Outcome}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString() ?? "Unknown");
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Pinata circuit breaker OPENED for 30s after {FailureCount} failures",
                        args.Outcome.Exception?.Message ?? "multiple failures");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Pinata circuit breaker CLOSED, resuming normal operation");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(_options.TimeoutSeconds))
            .Build();
    }

    /// <summary>
    /// Configures the HTTP client with base address, timeout, and authentication.
    /// </summary>
    /// <remarks>
    /// This method sets up the HTTP client with:
    /// <list type="bullet">
    /// <item><description>Base address from <see cref="PinataOptions.ApiUrl"/></description></item>
    /// <item><description>Timeout from <see cref="PinataOptions.TimeoutSeconds"/></description></item>
    /// <item><description>JWT Bearer authentication if JWT token is provided</description></item>
    /// <item><description>API key/secret headers if no JWT token is provided</description></item>
    /// </list>
    /// </remarks>
    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.ApiUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_options.JwtToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.JwtToken);
        }
        else if (!string.IsNullOrEmpty(_options.ApiKey) && !string.IsNullOrEmpty(_options.ApiSecret))
        {
            _httpClient.DefaultRequestHeaders.Add("pinata_api_key", _options.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("pinata_secret_api_key", _options.ApiSecret);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// In mock mode, generates a fake CID using SHA256 hash of the content without making API calls.
    /// In production mode, uploads the file to Pinata's pinning service via multipart form data.
    /// </remarks>
    public async Task<UploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(contentType);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty", nameof(fileName));
        }

        // File size validation (max 5GB as per Pinata limits)
        const long MaxFileSize = 5L * 1024 * 1024 * 1024; // 5GB
        if (content.CanSeek && content.Length > MaxFileSize)
        {
            throw new ArgumentException(
                $"File size ({content.Length / 1024.0 / 1024.0:F2} MB) exceeds maximum allowed size (5GB)",
                nameof(content));
        }

        if (_options.MockMode)
        {
            return await MockUploadAsync(content, fileName, contentType, metadata).ConfigureAwait(false);
        }

        using var formContent = new MultipartFormDataContent();

        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        formContent.Add(streamContent, "file", fileName);

        if (metadata != null)
        {
            var pinataMetadata = new PinataMetadataRequest
            {
                Name = fileName,
                KeyValues = metadata
            };
            formContent.Add(
                new StringContent(JsonSerializer.Serialize(pinataMetadata)),
                "pinataMetadata");
        }

        // Use resilience pipeline for retry and circuit breaker
        var response = await _resiliencePipeline.ExecuteAsync(
            async ct => await _httpClient.PostAsync("/pinning/pinFileToIPFS", formContent, ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PinataUploadResponse>(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Uploaded {FileName} to IPFS, CID: {Cid}, Size: {Size}",
            fileName, result!.IpfsHash, result.PinSize);

        return new UploadResult(
            result.IpfsHash,
            fileName,
            result.PinSize,
            contentType,
            IsPinned: true,
            result.Timestamp);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Convenience overload that wraps the byte array in a MemoryStream and delegates
    /// to the stream-based UploadAsync method.
    /// </remarks>
    public async Task<UploadResult> UploadAsync(
        byte[] data,
        string fileName,
        string contentType,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(data);
        return await UploadAsync(stream, fileName, contentType, metadata, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// In mock mode, returns a stream with mock content. In production, retrieves
    /// the content from the configured IPFS gateway.
    /// </remarks>
    public async Task<Stream> GetAsync(
        string cid,
        CancellationToken cancellationToken = default)
    {
        if (_options.MockMode)
        {
            _logger.LogInformation("MOCK: Retrieving CID {Cid}", cid);
            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Mock content for {cid}"));
        }

        var gatewayUrl = GetGatewayUrl(cid);
        var response = await _httpClient.GetAsync(gatewayUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// In mock mode, always returns true. In production, queries the Pinata API
    /// to check if the CID exists in the pin list.
    /// </remarks>
    public async Task<bool> IsPinnedAsync(
        string cid,
        CancellationToken cancellationToken = default)
    {
        if (_options.MockMode)
        {
            _logger.LogInformation("MOCK: Checking pin status for {Cid}", cid);
            return true;
        }

        var response = await _httpClient.GetAsync(
            $"/data/pinList?hashContains={cid}",
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return false;

        var result = await response.Content.ReadFromJsonAsync<PinataPinListResponse>(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result?.Count > 0;
    }

    /// <inheritdoc />
    /// <remarks>
    /// In mock mode, always returns true after logging. In production, sends a DELETE
    /// request to the Pinata unpin endpoint.
    /// </remarks>
    public async Task<bool> UnpinAsync(
        string cid,
        CancellationToken cancellationToken = default)
    {
        if (_options.MockMode)
        {
            _logger.LogInformation("MOCK: Unpinning {Cid}", cid);
            return true;
        }

        var response = await _httpClient.DeleteAsync(
            $"/pinning/unpin/{cid}",
            cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Unpinned {Cid}", cid);
            return true;
        }

        _logger.LogWarning("Failed to unpin {Cid}: {StatusCode}", cid, response.StatusCode);
        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// In mock mode, returns an empty list after logging. In production, queries
    /// the Pinata API for pinned content with optional name prefix filtering.
    /// </remarks>
    public async Task<IReadOnlyList<PinMetadata>> ListPinsAsync(
        string? namePrefix = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (_options.MockMode)
        {
            _logger.LogInformation("MOCK: Listing pins with prefix {Prefix}", namePrefix);
            return Array.Empty<PinMetadata>();
        }

        var url = $"/data/pinList?pageLimit={limit}";
        if (!string.IsNullOrEmpty(namePrefix))
        {
            url += $"&metadata[name]={Uri.EscapeDataString(namePrefix)}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PinataPinListResponse>(
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result?.Rows
            .Select(r => new PinMetadata(
                r.IpfsPinHash,
                r.Metadata?.Name ?? "",
                r.Size,
                r.DatePinned,
                r.Metadata?.KeyValues ?? new Dictionary<string, string>()))
            .ToList() ?? new List<PinMetadata>();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Constructs the gateway URL by appending the CID to the configured gateway base URL with /ipfs/ path.
    /// </remarks>
    public string GetGatewayUrl(string cid)
    {
        return $"{_options.GatewayUrl}/ipfs/{cid}";
    }

    /// <summary>
    /// Generates a mock upload result with a fake CID for testing purposes.
    /// </summary>
    /// <param name="content">The content stream to hash.</param>
    /// <param name="fileName">The filename.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A mock <see cref="UploadResult"/> with a generated CID.</returns>
    /// <remarks>
    /// The CID is generated by computing the SHA256 hash of the content and formatting
    /// it as a base58-style string starting with "Qm". This provides deterministic
    /// CIDs for the same content while maintaining the appearance of real IPFS CIDs.
    /// </remarks>
    private Task<UploadResult> MockUploadAsync(
        Stream content,
        string fileName,
        string contentType,
        Dictionary<string, string>? metadata)
    {
        // Generate a fake CID based on content hash
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(content);
        var cid = "Qm" + Convert.ToBase64String(hash)[..44].Replace("+", "").Replace("/", "");

        _logger.LogInformation(
            "MOCK: Uploaded {FileName}, CID: {Cid}",
            fileName, cid);

        return Task.FromResult(new UploadResult(
            cid,
            fileName,
            content.Length,
            contentType,
            IsPinned: true,
            DateTimeOffset.UtcNow));
    }

    // Internal DTOs for Pinata API responses
    private record PinataUploadResponse(
        [property: JsonPropertyName("IpfsHash")] string IpfsHash,
        [property: JsonPropertyName("PinSize")] long PinSize,
        [property: JsonPropertyName("Timestamp")] DateTimeOffset Timestamp);

    private record PinataMetadataRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("keyvalues")]
        public Dictionary<string, string> KeyValues { get; init; } = new();
    }

    private record PinataPinListResponse(
        [property: JsonPropertyName("count")] int Count,
        [property: JsonPropertyName("rows")] List<PinataPinRow> Rows);

    private record PinataPinRow(
        [property: JsonPropertyName("ipfs_pin_hash")] string IpfsPinHash,
        [property: JsonPropertyName("size")] long Size,
        [property: JsonPropertyName("date_pinned")] DateTimeOffset DatePinned,
        [property: JsonPropertyName("metadata")] PinataPinMetadata? Metadata);

    private record PinataPinMetadata(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("keyvalues")] Dictionary<string, string>? KeyValues);
}
