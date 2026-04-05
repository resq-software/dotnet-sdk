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

namespace ResQ.Storage;

/// <summary>
/// Configuration options for the Pinata IPFS client.
/// </summary>
/// <remarks>
/// This class provides configuration settings for the Pinata IPFS pinning service,
/// including API endpoints, authentication credentials, and operational parameters.
/// 
/// <para>
/// Authentication can be configured using either a JWT token (preferred) or API key/secret pair.
/// If both are provided, the JWT token takes precedence. For development and testing,
/// set <see cref="MockMode"/> to true to avoid making actual API calls.
/// </para>
/// 
/// <para>
/// Configuration can be loaded from appsettings.json, environment variables, or code.
/// Sensitive values like JWT tokens and API secrets should be stored securely
/// (e.g., in environment variables or secret managers) and not committed to source control.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configuration in appsettings.json
/// {
///   "PinataOptions": {
///     "ApiUrl": "https://api.pinata.cloud",
///     "GatewayUrl": "https://gateway.pinata.cloud/ipfs",
///     "JwtToken": "${PINATA_JWT}",
///     "MockMode": false,
///     "MaxFileSizeBytes": 104857600,
///     "TimeoutSeconds": 60
///   }
/// }
/// 
/// // Registration with dependency injection
/// services.Configure&lt;PinataOptions&gt;(configuration.GetSection("PinataOptions"));
/// 
/// // Or configure in code
/// services.Configure&lt;PinataOptions&gt;(options =>
/// {
///     options.JwtToken = Environment.GetEnvironmentVariable("PINATA_JWT");
///     options.MockMode = false;
/// });
/// </code>
/// </example>
public class PinataOptions
{
    /// <summary>
    /// Gets or sets the Pinata API endpoint URL.
    /// </summary>
    /// <value>The API base URL. Default is "https://api.pinata.cloud".</value>
    /// <remarks>
    /// This is the base URL for all Pinata API calls. The default value points to
    /// Pinata's production API. Change this only if using a custom Pinata deployment
    /// or proxy.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.ApiUrl = "https://api.pinata.cloud";
    /// </code>
    /// </example>
    public string ApiUrl { get; set; } = "https://api.pinata.cloud";

    /// <summary>
    /// Gets or sets the IPFS gateway URL for retrieving content.
    /// </summary>
    /// <value>The gateway base URL. Default is "https://gateway.pinata.cloud/ipfs".</value>
    /// <remarks>
    /// This URL is used to construct public gateway URLs for uploaded content.
    /// The default uses Pinata's public gateway. For production use with high traffic,
    /// consider using a dedicated gateway or your own IPFS node.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Use Pinata's dedicated gateway (requires paid plan)
    /// options.GatewayUrl = "https://your-gateway.mypinata.cloud/ipfs";
    /// 
    /// // Use public IPFS gateway
    /// options.GatewayUrl = "https://ipfs.io/ipfs";
    /// </code>
    /// </example>
    public string GatewayUrl { get; set; } = "https://gateway.pinata.cloud/ipfs";

    /// <summary>
    /// Gets or sets the Pinata JWT token for authentication.
    /// </summary>
    /// <value>The JWT token string, or null if not using JWT authentication.</value>
    /// <remarks>
    /// JWT authentication is the preferred method and provides full API access.
    /// Generate a JWT token from your Pinata account dashboard. When provided,
    /// this takes precedence over API key/secret authentication.
    /// 
    /// <para>
    /// Store this value securely (e.g., in environment variables) and never commit
    /// it to source control.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.JwtToken = Environment.GetEnvironmentVariable("PINATA_JWT");
    /// </code>
    /// </example>
    public string? JwtToken { get; set; }

    /// <summary>
    /// Gets or sets the Pinata API key for authentication.
    /// </summary>
    /// <value>The API key string, or null if not using API key authentication.</value>
    /// <remarks>
    /// API key authentication is an alternative to JWT tokens. When using API keys,
    /// both <see cref="ApiKey"/> and <see cref="ApiSecret"/> must be provided.
    /// JWT authentication is preferred when available.
    /// 
    /// <para>
    /// Store this value securely and never commit it to source control.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.ApiKey = Environment.GetEnvironmentVariable("PINATA_API_KEY");
    /// options.ApiSecret = Environment.GetEnvironmentVariable("PINATA_API_SECRET");
    /// </code>
    /// </example>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the Pinata API secret for authentication.
    /// </summary>
    /// <value>The API secret string, or null if not using API key authentication.</value>
    /// <remarks>
    /// The API secret must be paired with <see cref="ApiKey"/> for authentication.
    /// This is used as an alternative to JWT tokens. JWT authentication is preferred.
    /// 
    /// <para>
    /// Store this value securely and never commit it to source control.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.ApiKey = Environment.GetEnvironmentVariable("PINATA_API_KEY");
    /// options.ApiSecret = Environment.GetEnvironmentVariable("PINATA_API_SECRET");
    /// </code>
    /// </example>
    public string? ApiSecret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use mock mode for testing.
    /// </summary>
    /// <value>True to use mock implementation; false to use real Pinata API. Default is true.</value>
    /// <remarks>
    /// When enabled, the client will generate fake CIDs and not make actual API calls.
    /// This is useful for development and testing without consuming Pinata credits
    /// or requiring network access. Set to false for production deployments.
    /// 
    /// <para>
    /// Mock mode generates deterministic CIDs using SHA256 hashes of the content,
    /// allowing for consistent testing scenarios.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Development/testing
    /// options.MockMode = true;
    /// 
    /// // Production
    /// options.MockMode = false;
    /// </code>
    /// </example>
    public bool MockMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum file size allowed for uploads.
    /// </summary>
    /// <value>Maximum file size in bytes. Default is 100MB (104,857,600 bytes).</value>
    /// <remarks>
    /// Files larger than this limit will be rejected before uploading.
    /// The default is 100MB which is suitable for most use cases.
    /// Increase this if you need to upload larger files (e.g., high-resolution videos).
    /// 
    /// <para>
    /// Note: Pinata's free tier has its own upload limits. Check your Pinata plan
    /// for actual limits regardless of this setting.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Allow files up to 500MB
    /// options.MaxFileSizeBytes = 500 * 1024 * 1024;
    /// 
    /// // Limit to 10MB for stricter control
    /// options.MaxFileSizeBytes = 10 * 1024 * 1024;
    /// </code>
    /// </example>
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the HTTP request timeout in seconds.
    /// </summary>
    /// <value>Timeout duration in seconds. Default is 60 seconds.</value>
    /// <remarks>
    /// This controls how long the HTTP client will wait for API responses.
    /// Increase this for slow networks or large file uploads. Decrease for
    /// faster failure detection in high-availability scenarios.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Increase timeout for large uploads on slow connections
    /// options.TimeoutSeconds = 120;
    /// 
    /// // Decrease for faster failure in responsive environments
    /// options.TimeoutSeconds = 30;
    /// </code>
    /// </example>
    public int TimeoutSeconds { get; set; } = 60;
}
