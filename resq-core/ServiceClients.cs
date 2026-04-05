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

using System.Text;
using System.Text.Json;

namespace ResQ.Core;

/// <summary>
/// Client for the HCE (Human-Centric Emergency) coordination layer.
/// </summary>
/// <remarks>
/// Provides methods for sending telemetry and reporting detections to the
/// HCE coordination service.
/// </remarks>
/// <example>
/// <code>
/// var client = new HceClient("http://localhost:3000");
///
/// var success = await client.SendTelemetryAsync(telemetryPacket);
/// if (success)
/// {
///     Console.WriteLine("Telemetry sent successfully");
/// }
/// </code>
/// </example>
public sealed class HceClient : IDisposable
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="HceClient"/> class.
    /// </summary>
    /// <param name="baseUrl">Base URL of the HCE service.</param>
    public HceClient(string baseUrl = "http://localhost:3000")
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    /// Sends telemetry data from a drone to the HCE service.
    /// </summary>
    /// <param name="packet">The telemetry packet to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the telemetry was accepted.</returns>
    public async Task<bool> SendTelemetryAsync(
        TelemetryPacket packet,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(packet);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v1/telemetry", content, ct)
            .ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Reports a critical detection to the HCE service.
    /// </summary>
    /// <param name="detection">The detection to report.</param>
    /// <param name="droneId">ID of the drone that made the detection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the report was accepted.</returns>
    public async Task<bool> ReportDetectionAsync(
        Detection detection,
        string droneId,
        CancellationToken ct = default)
    {
        var payload = new { detection, droneId };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v1/incidents/report", content, ct)
            .ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public void Dispose() => _httpClient.Dispose();
}

/// <summary>
/// Client for the PDIE (Predictive Disaster Intelligence Engine) service.
/// </summary>
/// <remarks>
/// Provides access to predictive alerts and risk assessments from the
/// intelligence layer.
/// </remarks>
/// <example>
/// <code>
/// var client = new PdieClient("http://localhost:8000");
///
/// var alerts = await client.GetPreAlertsAsync("sector-7");
/// foreach (var alert in alerts)
/// {
///     Console.WriteLine($"Alert: {alert.PredictedDisasterType} ({alert.Probability:P})");
/// }
/// </code>
/// </example>
public sealed class PdieClient : IDisposable
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdieClient"/> class.
    /// </summary>
    /// <param name="baseUrl">Base URL of the PDIE service.</param>
    public PdieClient(string baseUrl = "http://localhost:8000")
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    /// Gets current pre-alerts, optionally filtered by sector.
    /// </summary>
    /// <param name="sectorId">Optional sector filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of active pre-alerts.</returns>
    public async Task<List<PreAlert>> GetPreAlertsAsync(
        string? sectorId = null,
        CancellationToken ct = default)
    {
        var url = sectorId != null
            ? $"/api/v1/alerts?sector={sectorId}"
            : "/api/v1/alerts";

        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<PreAlert>>(json) ?? new();
    }

    /// <inheritdoc />
    public void Dispose() => _httpClient.Dispose();
}

/// <summary>
/// Client for the DTSOP (Drone Tactical Strategy Optimization) service.
/// </summary>
/// <remarks>
/// Provides access to optimization strategies and deployment recommendations
/// from the simulation layer.
/// </remarks>
/// <example>
/// <code>
/// var client = new DtsopClient("http://localhost:9000");
///
/// var strategy = await client.RequestStrategyAsync("scenario-001");
/// Console.WriteLine($"Coverage: {strategy.EstimatedCoveragePercent:F1}%");
/// </code>
/// </example>
public sealed class DtsopClient : IDisposable
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DtsopClient"/> class.
    /// </summary>
    /// <param name="baseUrl">Base URL of the DTSOP service.</param>
    public DtsopClient(string baseUrl = "http://localhost:9000")
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    /// Requests an optimization strategy for a scenario.
    /// </summary>
    /// <param name="scenarioId">ID of the scenario to optimize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The optimization strategy.</returns>
    public async Task<OptimizationStrategy> RequestStrategyAsync(
        string scenarioId,
        CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(
            $"/api/v1/strategies/{scenarioId}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<OptimizationStrategy>(json)
            ?? throw new InvalidOperationException("Failed to parse strategy");
    }

    /// <inheritdoc />
    public void Dispose() => _httpClient.Dispose();
}
