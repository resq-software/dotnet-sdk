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
using Microsoft.Extensions.Logging;

namespace ResQ.Clients;

/// <summary>
/// HTTP client for infrastructure-api (Rust/Axum) service.
/// Provides methods to upload evidence, record blockchain events, and manage incidents.
/// Inherits resilience patterns from <see cref="BaseServiceClient"/>.
/// </summary>
public class InfrastructureApiClient : BaseServiceClient
{
    protected override string ServiceName => "Infrastructure API";

    public InfrastructureApiClient(string baseUrl = "http://localhost:5000", HttpMessageHandler? handler = null, ILogger? logger = null)
        : base(baseUrl, handler, logger)
    {
    }

    /// <summary>
    /// Authenticates with infrastructure-api to get a JWT token.
    /// Sets the Authorization header for subsequent requests.
    /// </summary>
    public async Task<bool> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        // F-P9-02: fail fast on null/empty credentials
        ArgumentException.ThrowIfNullOrWhiteSpace(username, nameof(username));
        ArgumentException.ThrowIfNullOrWhiteSpace(password, nameof(password));

        try
        {
            // F-P9-01: route through resilience pipeline (10s timeout + circuit breaker, no retry)
            var response = await ExecuteWithResilienceAsync(
                HttpMethod.Post,
                token => Http.PostAsJsonAsync("/login", new { username, password }, token),
                ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // P6-SI-01: clear stale Authorization on non-success — symmetric with exception path
                AuthorizationHeader = null;
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<InfraAuthResponse>()
                .ConfigureAwait(false);

            if (result?.Token != null)
            {
                AuthorizationHeader = new AuthenticationHeaderValue("Bearer", result.Token);
                return true;
            }

            AuthorizationHeader = null; // N5: clear stale token on re-auth failure
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // P4-SI-02: clear stale Authorization header on any auth failure
            AuthorizationHeader = null;
            return false;
        }
    }

    /// <summary>
    /// Uploads an image to IPFS via infrastructure-api.
    /// Uses timeout and circuit-breaker handling without replaying the upload on failure.
    /// </summary>
    public async Task<UploadResponse> UploadImageAsync(byte[] imageData, string fileName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(imageData, nameof(imageData));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        if (imageData.Length == 0)
            throw new ArgumentException("Image data cannot be empty", nameof(imageData));

        // Non-idempotent uploads are no longer retried, so a single multipart body is safe here.
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(imageData), "file", fileName);

        var response = await SendAsync(
            HttpMethod.Post,
            "/uploadImage",
            content,
            ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<UploadResponse>(cancellationToken: ct)
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Upload response was null");
    }

    /// <summary>
    /// Records a blockchain event via infrastructure-api Neo N3 adapter.
    /// Uses timeout and circuit-breaker handling without replaying the mutation on failure.
    /// </summary>
    public async Task<BlockchainEventResponse> RecordEventAsync(BlockchainEventRequest evt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evt, nameof(evt));

        var response = await SendAsync(
            HttpMethod.Post,
            "/blockchain/events",
            JsonContent.Create(new
            {
                event_id = evt.EventId,
                event_type = evt.EventType,
                payload = evt.Payload,
                evidence_cid = evt.IpfsCid,
                drone_id = evt.DroneId,
                timestamp = evt.Timestamp
            }),
            ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BlockchainEventResponse>(cancellationToken: ct)
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Blockchain response was null");
    }

    /// <summary>
    /// Creates an incident record.
    /// Uses timeout and circuit-breaker handling without replaying the mutation on failure.
    /// </summary>
    public async Task<IncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var response = await SendAsync(
            HttpMethod.Post,
            "/incidents",
            JsonContent.Create(request),
            ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IncidentResponse>(cancellationToken: ct)
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Incident response was null");
    }

    /// <summary>
    /// Gets infrastructure-api health status.
    /// Includes retry logic for transient read failures.
    /// </summary>
    public async Task<HealthResponse> GetHealthAsync(CancellationToken ct = default)
    {
        var response = await SendAsync(HttpMethod.Get, "/health", cancellationToken: ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken: ct)
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Health response was null");
    }
}

/// <summary>
/// Response from an IPFS upload operation.
/// </summary>
public record UploadResponse(string Cid, long Size, string GatewayUrl);

/// <summary>
/// Request to record a blockchain event via infrastructure-api.
/// </summary>
public record BlockchainEventRequest(
    string EventId,
    string EventType,
    string Payload,
    string? IpfsCid,
    string? DroneId,
    long Timestamp
);

/// <summary>
/// Response from recording a blockchain event.
/// </summary>
public record BlockchainEventResponse(
    string EventId,
    string EventType,
    long Timestamp,
    string TxHash
);

/// <summary>
/// Request to create a new incident.
/// </summary>
public record CreateIncidentRequest(
    string IncidentType,
    string Severity,
    LocationDto? Location,
    string? Description
);

/// <summary>
/// Response from creating or retrieving an incident.
/// </summary>
public record IncidentResponse(
    string Id,
    string IncidentType,
    string Severity,
    string Status,
    string CreatedAt
);

/// <summary>
/// Health check response from infrastructure-api.
/// </summary>
public record HealthResponse(
    string Status,
    bool Pinata,
    bool Gemini,
    bool Blockchain
);

/// <summary>
/// Geographic location with coordinates and altitude.
/// </summary>
public record LocationDto(double Latitude, double Longitude, double Altitude);

/// <summary>
/// JWT response from infrastructure-api /login endpoint.
/// </summary>
public record InfraAuthResponse(string Token);
