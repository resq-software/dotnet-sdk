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

namespace ResQ.Core;

/// <summary>
/// Represents a pre-alert from the PDIE (Predictive Disaster Intelligence Engine).
/// </summary>
/// <remarks>
/// Pre-alerts are predictions of potential disasters before they occur, allowing
/// for proactive response preparation. They include probability, timing, and
/// severity estimates.
/// </remarks>
/// <example>
/// <code>
/// var alert = new PreAlert
/// {
///     AlertId = "pa-001",
///     SectorId = "sector-7",
///     PredictedDisasterType = DisasterType.Wildfire,
///     Probability = 0.85f,
///     ForecastHorizonHours = 24,
///     Severity = AlertSeverity.High
/// };
/// </code>
/// </example>
public record PreAlert
{
    /// <summary>Unique identifier for this pre-alert.</summary>
    public required string AlertId { get; init; }

    /// <summary>Geographic sector this alert applies to.</summary>
    public required string SectorId { get; init; }

    /// <summary>Predicted type of disaster.</summary>
    public DisasterType PredictedDisasterType { get; init; }

    /// <summary>Probability of the disaster occurring (0.0 to 1.0).</summary>
    public float Probability { get; init; }

    /// <summary>Forecast time horizon in hours.</summary>
    public int ForecastHorizonHours { get; init; }

    /// <summary>Severity level of the predicted disaster.</summary>
    public AlertSeverity Severity { get; init; }

    /// <summary>UTC timestamp when the pre-alert was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents an optimization strategy from DTSOP (Drone Tactical Strategy Optimization).
/// </summary>
/// <remarks>
/// Optimization strategies provide recommended drone deployments, coverage estimates,
/// and response time predictions for disaster scenarios.
/// </remarks>
/// <example>
/// <code>
/// var strategy = new OptimizationStrategy
/// {
///     StrategyId = "strat-001",
///     ScenarioId = "scen-001",
///     EstimatedCoveragePercent = 85.5,
///     EstimatedResponseTimeMinutes = 12.3,
///     Deployments = new List&lt;DeploymentRecommendation&gt;
///     {
///         new() { DroneId = "drn-001", TargetPosition = location, MissionType = "Search", Priority = 1 }
///     }
/// };
/// </code>
/// </example>
public record OptimizationStrategy
{
    /// <summary>Unique identifier for this strategy.</summary>
    public required string StrategyId { get; init; }

    /// <summary>ID of the scenario this strategy applies to.</summary>
    public required string ScenarioId { get; init; }

    /// <summary>List of recommended drone deployments.</summary>
    public List<DeploymentRecommendation> Deployments { get; init; } = new();

    /// <summary>Estimated area coverage percentage.</summary>
    public double EstimatedCoveragePercent { get; init; }

    /// <summary>Estimated response time in minutes.</summary>
    public double EstimatedResponseTimeMinutes { get; init; }

    /// <summary>Confidence score for this strategy (0.0 to 1.0).</summary>
    public double ConfidenceScore { get; init; }

    /// <summary>UTC timestamp when the strategy was generated.</summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a single drone deployment recommendation.
/// </summary>
/// <remarks>
/// Part of an optimization strategy, specifying which drone should be deployed
/// where and for what mission.
/// </remarks>
/// <example>
/// <code>
/// var deployment = new DeploymentRecommendation
/// {
///     DroneId = "drn-001",
///     TargetPosition = new Location(37.7749, -122.4194),
///     MissionType = "Search",
///     Priority = 1,
///     Rationale = "High probability survivor location"
/// };
/// </code>
/// </example>
public record DeploymentRecommendation
{
    /// <summary>ID of the drone to deploy.</summary>
    public required string DroneId { get; init; }

    /// <summary>Target position for deployment.</summary>
    public required Location TargetPosition { get; init; }

    /// <summary>Type of mission to execute.</summary>
    public required string MissionType { get; init; }

    /// <summary>Priority level (lower number = higher priority).</summary>
    public int Priority { get; init; }

    /// <summary>Explanation for this recommendation.</summary>
    public string? Rationale { get; init; }
}
