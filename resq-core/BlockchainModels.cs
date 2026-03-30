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
/// Represents a blockchain event for immutable logging.
/// </summary>
/// <remarks>
/// Blockchain events provide tamper-proof records of system activities.
/// They can include metadata and links to off-chain evidence.
/// </remarks>
/// <example>
/// <code>
/// var evt = new BlockchainEvent
/// {
///     EventId = "evt-001",
///     EventType = BlockchainEventType.IncidentDetected,
///     Location = new Location(37.7749, -122.4194),
///     EvidenceCid = "Qmabc123...",
///     Metadata = new Dictionary&lt;string, object&gt;
///     {
///         ["incidentId"] = "inc-001",
///         ["severity"] = "High"
///     }
/// };
/// </code>
/// </example>
public record BlockchainEvent
{
    /// <summary>Unique identifier for this event.</summary>
    public required string EventId { get; init; }

    /// <summary>Type of blockchain event.</summary>
    public BlockchainEventType EventType { get; init; }

    /// <summary>UTC timestamp when the event occurred.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Geographic location associated with the event, if applicable.</summary>
    public Location? Location { get; init; }

    /// <summary>IPFS CID of associated evidence, if any.</summary>
    public string? EvidenceCid { get; init; }

    /// <summary>Drone ID associated with the event, if applicable.</summary>
    public string? DroneId { get; init; }

    /// <summary>Additional metadata as key-value pairs.</summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents the result of a blockchain transaction.
/// </summary>
/// <remarks>
/// Contains the transaction hash, block information, gas consumption, and status.
/// </remarks>
/// <example>
/// <code>
/// var result = new TransactionResult
/// {
///     TxHash = "0xabc123...",
///     BlockHeight = 1234567,
///     GasConsumed = 1000000,
///     Status = TransactionStatus.Confirmed
/// };
/// </code>
/// </example>
public record TransactionResult
{
    /// <summary>Hexadecimal transaction hash.</summary>
    public required string TxHash { get; init; }

    /// <summary>Block height where transaction was included.</summary>
    public ulong? BlockHeight { get; init; }

    /// <summary>Gas consumed by the transaction.</summary>
    public ulong GasConsumed { get; init; }

    /// <summary>Current transaction status.</summary>
    public TransactionStatus Status { get; init; }

    /// <summary>Error message if transaction failed.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Configuration for Neo N3 blockchain connection.
/// </summary>
/// <remarks>
/// Provides settings for connecting to the Neo N3 blockchain including
/// RPC endpoint, contract address, and network identification.
/// </remarks>
/// <example>
/// <code>
/// var config = new NeoConfig
/// {
///     RpcUrl = "https://testnet1.neo.coz.io:443",
///     ContractHash = "0x8d35a57f8c01156527c92ebbb4d772fa9574cbf4",
///     NetworkMagic = 894710606,
///     MockMode = false
/// };
/// </code>
/// </example>
public record NeoConfig
{
    /// <summary>Neo RPC endpoint URL.</summary>
    public string RpcUrl { get; init; } = "https://testnet1.neo.coz.io:443";

    /// <summary>Smart contract script hash.</summary>
    public string ContractHash { get; init; } = "0x0000000000000000000000000000000000000000";

    /// <summary>Network magic number (TestNet = 877933390).</summary>
    public uint NetworkMagic { get; init; } = 877933390;

    /// <summary>Enable mock mode for testing.</summary>
    public bool MockMode { get; init; } = true;
}
