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

using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace ResQ.Clients;

/// <summary>
/// HTTP client for coordination-hce (Node.js/Elysia) service.
/// Provides methods to send telemetry, report incidents, and query fleet status.
/// Inherits resilience patterns from <see cref="BaseServiceClient"/>.
/// </summary>
public class CoordinationHceClient : BaseServiceClient
{
    protected override string ServiceName => "Coordination HCE";

    public CoordinationHceClient(string baseUrl = "http://localhost:3000", HttpMessageHandler? handler = null, ILogger? logger = null)
        : base(baseUrl, handler, logger)
    {
    }

    /// <summary>
    /// Authenticates with HCE to get JWT token (if auth is enabled).
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
                token => Http.PostAsJsonAsync("/auth/login", new { username, password }, token),
                ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // P6-SI-01: clear stale credentials on 401/403 — symmetric with exception path
                AuthorizationHeader = null;
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>()
                .ConfigureAwait(false);
            var jwtToken = result?.Token;

            if (jwtToken != null)
            {
                AuthorizationHeader = new AuthenticationHeaderValue("Bearer", jwtToken);
            }
            else
            {
                // Clear any stale token from a previous successful auth
                AuthorizationHeader = null;
            }

            return jwtToken != null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // P4-SI-01: clear stale token on any auth failure so subsequent requests
            // don't silently use an expired/revoked credential
            AuthorizationHeader = null;
            return false;
        }
    }

    /// <summary>
    /// Sends a batch of telemetry packets from a drone.
    /// Uses timeout and circuit-breaker handling without replaying the mutation on failure.
    /// </summary>
    public async Task SendTelemetryBatchAsync(TelemetryBatchRequest batch, CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentException.ThrowIfNullOrWhiteSpace(batch.DroneId, nameof(batch.DroneId));

        if (batch.Packets == null || batch.Packets.Count == 0)
        {
            throw new ArgumentException("Packets cannot be null or empty", nameof(batch.Packets));
        }

        var response = await SendAsync(
            HttpMethod.Post,
            "/v1/telemetry/batch",
            JsonContent.Create(batch),
            ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Reports an incident to HCE.
    /// Uses timeout and circuit-breaker handling without replaying the mutation on failure.
    /// </summary>
    public async Task<IncidentAck> ReportIncidentAsync(ReportIncidentRequest incident, CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(incident.Location);

        // Validate latitude bounds (-90 to 90)
        if (incident.Location.Latitude < -90.0 || incident.Location.Latitude > 90.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(incident.Location.Latitude),
                incident.Location.Latitude,
                "Latitude must be between -90 and 90 degrees");
        }

        // Validate longitude bounds (-180 to 180)
        if (incident.Location.Longitude < -180.0 || incident.Location.Longitude > 180.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(incident.Location.Longitude),
                incident.Location.Longitude,
                "Longitude must be between -180 and 180 degrees");
        }

        var response = await SendAsync(
            HttpMethod.Post,
            "/v1/incident",
            JsonContent.Create(incident),
            ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IncidentAck>(cancellationToken: ct)
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Incident ack was null");
    }

    /// <summary>
    /// Gets the status of a fleet.
    /// Includes retry logic for transient read failures.
    /// </summary>
    public async Task<FleetStatus> GetFleetStatusAsync(string fleetId, CancellationToken ct = default)
    {
        // P5-F01: validate and URL-encode fleetId
        ArgumentException.ThrowIfNullOrWhiteSpace(fleetId, nameof(fleetId));

        var response = await SendAsync(
            HttpMethod.Get,
            $"/fleet/{Uri.EscapeDataString(fleetId)}",
            cancellationToken: ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FleetStatus>(cancellationToken: ct)
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Fleet status was null");
    }

    /// <summary>
    /// Gets HCE health status.
    /// Includes retry logic for transient read failures.
    /// </summary>
    public async Task<HceHealthResponse> GetHealthAsync(CancellationToken ct = default)
    {
        var response = await SendAsync(HttpMethod.Get, "/health", cancellationToken: ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<HceHealthResponse>(cancellationToken: ct)
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Health response was null");
    }
}

/// <summary>
/// Response from the HCE authentication endpoint.
/// </summary>
public record AuthResponse(string Token);

/// <summary>
/// Request containing a batch of telemetry packets from a drone.
/// </summary>
/// <param name="DroneId">The unique identifier of the drone.</param>
/// <param name="Packets">List of telemetry packets in this batch.</param>
/// <param name="Detections">Optional list of AI detections from this batch.</param>
public record TelemetryBatchRequest(
    string DroneId,
    List<TelemetryPacket> Packets,
    List<Detection>? Detections = null
);

/// <summary>
/// A single telemetry packet from a drone.
/// </summary>
/// <param name="DroneId">Unique identifier of the drone (required by HCE per-packet schema).</param>
/// <param name="Latitude">Latitude in decimal degrees.</param>
/// <param name="Longitude">Longitude in decimal degrees.</param>
/// <param name="Altitude">Altitude in meters above sea level.</param>
/// <param name="Battery">Battery percentage (0-100).</param>
/// <param name="FlightMode">Current flight mode (e.g., "IDLE", "ARMED", "AUTO").</param>
/// <param name="Timestamp">Unix timestamp in seconds.</param>
public record TelemetryPacket(
    string DroneId,
    double Latitude,
    double Longitude,
    double Altitude,
    double Battery,
    string FlightMode,
    long Timestamp
);

/// <summary>
/// A detection result from the drone's AI system.
/// </summary>
/// <param name="Type">Type of detection (e.g., "FIRE", "FLOOD", "PERSON").</param>
/// <param name="Confidence">AI confidence score (0.0 to 1.0).</param>
/// <param name="Location">Geographic location of the detection.</param>
/// <param name="Timestamp">Unix timestamp when detection occurred.</param>
public record Detection(
    string Type,
    double Confidence,
    LocationDto Location,
    long Timestamp
);

/// <summary>
/// Request to report an incident to HCE.
/// </summary>
/// <param name="IncidentType">Type of incident (e.g., "FIRE", "FLOOD").</param>
/// <param name="Severity">Severity level (e.g., "LOW", "MEDIUM", "HIGH", "CRITICAL").</param>
/// <param name="Location">Geographic location of the incident.</param>
/// <param name="Description">Optional human-readable description.</param>
public record ReportIncidentRequest(
    string IncidentType,
    string Severity,
    LocationDto Location,
    string? Description
);

/// <summary>
/// Acknowledgment response from incident report.
/// </summary>
/// <param name="IncidentId">Unique identifier for the reported incident.</param>
/// <param name="Status">Current status of the incident.</param>
public record IncidentAck(
    string IncidentId,
    string Status
);

/// <summary>
/// Status response for a fleet of drones.
/// </summary>
/// <param name="FleetId">Unique identifier for the fleet.</param>
/// <param name="ActiveDrones">Number of currently active drones in the fleet.</param>
/// <param name="TotalMissions">Total number of missions completed by the fleet.</param>
public record FleetStatus(
    string FleetId,
    int ActiveDrones,
    int TotalMissions
);

/// <summary>
/// Health check response from HCE service.
/// </summary>
/// <param name="Status">Health status (e.g., "healthy", "degraded").</param>
public record HceHealthResponse(string Status);
