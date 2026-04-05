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

namespace ResQ.Blockchain;

/// <summary>
/// Configuration options for the Neo N3 blockchain client.
/// </summary>
/// <remarks>
/// This class provides configuration settings for connecting to the Neo N3 blockchain,
/// including RPC endpoint, network identification, contract addresses, and operational
/// parameters such as timeouts and retry logic.
/// 
/// <para>
/// For development and testing, set <see cref="MockMode"/> to true to use the mock
/// client implementation. For production, configure the <see cref="RpcUrl"/> to point
/// to a reliable Neo N3 RPC endpoint and provide the wallet path for transaction signing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configuration in appsettings.json
/// {
///   "NeoClientOptions": {
///     "RpcUrl": "https://testnet1.neo.coz.io:443",
///     "NetworkMagic": 894710606,
///     "ContractHash": "0x8d35a57f8c01156527c92ebbb4d772fa9574cbf4",
///     "MockMode": false,
///     "ConfirmationTimeoutSeconds": 30,
///     "MaxRetryAttempts": 3
///   }
/// }
/// 
/// // Registration with dependency injection
/// services.Configure&lt;NeoClientOptions&gt;(configuration.GetSection("NeoClientOptions"));
/// 
/// // Usage in a service
/// public class MyService
/// {
///     private readonly NeoClientOptions _options;
///     
///     public MyService(IOptions&lt;NeoClientOptions&gt; options)
///     {
///         _options = options.Value;
///     }
/// }
/// </code>
/// </example>
public class NeoClientOptions
{
    /// <summary>
    /// Gets or sets the Neo N3 RPC endpoint URL.
    /// </summary>
    /// <value>The RPC endpoint URL. Default is "http://localhost:10332".</value>
    /// <remarks>
    /// This should point to a Neo N3 RPC node. For testnet, use a public endpoint like
    /// "https://testnet1.neo.coz.io:443". For mainnet, use a reliable node provider.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.RpcUrl = "https://testnet1.neo.coz.io:443";
    /// </code>
    /// </example>
    public string RpcUrl { get; set; } = "http://localhost:10332";

    /// <summary>
    /// Gets or sets the network magic number for identifying the Neo network.
    /// </summary>
    /// <value>The network magic number. Default is 894710606 (TestNet).</value>
    /// <remarks>
    /// The network magic is used to identify which Neo network to connect to:
    /// <list type="bullet">
    /// <item><description>MainNet: 860833102</description></item>
    /// <item><description>TestNet: 894710606</description></item>
    /// <item><description>PrivateNet: Custom value</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // MainNet
    /// options.NetworkMagic = 860833102;
    /// 
    /// // TestNet (default)
    /// options.NetworkMagic = 894710606;
    /// </code>
    /// </example>
    public uint NetworkMagic { get; set; } = 894710606; // TestNet default

    /// <summary>
    /// Gets or sets the smart contract script hash for ResQ event recording.
    /// </summary>
    /// <value>The 40-character hexadecimal contract script hash.</value>
    /// <remarks>
    /// This is the hash of the deployed smart contract that handles ResQ events.
    /// It must be set to a valid contract hash for blockchain operations to succeed.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.ContractHash = "0x8d35a57f8c01156527c92ebbb4d772fa9574cbf4";
    /// </code>
    /// </example>
    public string ContractHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path to the wallet for transaction signing.
    /// </summary>
    /// <value>The absolute or relative path to the wallet file, or null if not using a wallet file.</value>
    /// <remarks>
    /// This should point to a Neo N3 compatible wallet file (e.g., .json or .db3 format).
    /// The wallet will be used to sign transactions before submission to the blockchain.
    /// For security, ensure the wallet file has appropriate access restrictions.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.WalletPath = "/secure/wallets/resq-wallet.json";
    /// </code>
    /// </example>
    public string? WalletPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use mock mode for testing.
    /// </summary>
    /// <value>True to use mock implementation; false to use real blockchain. Default is true.</value>
    /// <remarks>
    /// When enabled, the client will use <see cref="MockNeoClient"/> instead of making real
    /// blockchain calls. This is useful for development and testing without requiring a live
    /// blockchain connection. Set to false for production deployments.
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
    /// Gets or sets the timeout for waiting for transaction confirmation.
    /// </summary>
    /// <value>The timeout in seconds. Default is 30 seconds.</value>
    /// <remarks>
    /// This defines how long the client will wait for a transaction to be confirmed
    /// on the blockchain before timing out. Increase this value if operating on a
    /// network with slower block times or high congestion.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Wait up to 60 seconds for confirmation
    /// options.ConfirmationTimeoutSeconds = 60;
    /// </code>
    /// </example>
    public int ConfirmationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed transactions.
    /// </summary>
    /// <value>The maximum retry attempts. Default is 3.</value>
    /// <remarks>
    /// If a blockchain transaction fails due to transient errors (network issues,
    /// temporary node unavailability), the client will retry up to this many times
    /// before giving up. Set to 0 to disable retries.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Retry up to 5 times
    /// options.MaxRetryAttempts = 5;
    /// 
    /// // Disable retries
    /// options.MaxRetryAttempts = 0;
    /// </code>
    /// </example>
    public int MaxRetryAttempts { get; set; } = 3;
}
