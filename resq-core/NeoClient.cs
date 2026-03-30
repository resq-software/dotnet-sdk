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
/// Client for interacting with the Neo N3 blockchain.
/// </summary>
/// <remarks>
/// Provides methods for recording events, verifying attestations, and querying
/// transaction status on the Neo N3 blockchain. Supports both real blockchain
/// operations and mock mode for testing.
/// </remarks>
/// <example>
/// <code>
/// using var client = new NeoClient(new NeoConfig { MockMode = true });
///
/// var evt = new BlockchainEvent
/// {
///     EventId = "evt-001",
///     EventType = BlockchainEventType.IncidentDetected
/// };
///
/// var result = await client.RecordEventAsync(evt);
/// Console.WriteLine($"Transaction: {result.TxHash}");
/// </code>
/// </example>
public sealed class NeoClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly NeoConfig _config;
    private ulong _requestId;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoClient"/> class.
    /// </summary>
    /// <param name="config">Optional configuration. Uses defaults if not provided.</param>
    public NeoClient(NeoConfig? config = null)
    {
        _config = config ?? new NeoConfig();
        _httpClient = new HttpClient { BaseAddress = new Uri(_config.RpcUrl) };
    }

    /// <summary>
    /// Records an event on the Neo N3 blockchain.
    /// </summary>
    /// <param name="evt">The event to record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Transaction result including hash and status.</returns>
    public async Task<TransactionResult> RecordEventAsync(
        BlockchainEvent evt,
        CancellationToken ct = default)
    {
        if (_config.MockMode)
        {
            await Task.Delay(100, ct).ConfigureAwait(false);
            return new TransactionResult
            {
                TxHash = $"0x{Guid.NewGuid():N}",
                BlockHeight = (ulong)Random.Shared.Next(1000000, 9999999),
                GasConsumed = (ulong)Random.Shared.Next(100000, 1000000),
                Status = TransactionStatus.Confirmed
            };
        }

        var response = await RpcCallAsync("invokefunction", new object[]
        {
            _config.ContractHash,
            "recordEvent",
            new object[]
            {
                new { type = "String", value = evt.EventId },
                new { type = "Integer", value = (int)evt.EventType },
                new { type = "Integer", value = evt.Timestamp.ToUnixTimeSeconds() }
            }
        }, ct).ConfigureAwait(false);

        var txHash = response.GetProperty("tx").GetProperty("hash").GetString();

        return new TransactionResult
        {
            TxHash = txHash ?? throw new InvalidOperationException("Missing tx hash"),
            Status = TransactionStatus.Pending
        };
    }

    /// <summary>
    /// Verifies a location attestation on the blockchain.
    /// </summary>
    /// <param name="droneId">The drone identifier.</param>
    /// <param name="location">The location to verify.</param>
    /// <param name="timestamp">The timestamp of the attestation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the attestation is valid.</returns>
    public async Task<bool> VerifyLocationAttestationAsync(
        string droneId,
        Location location,
        DateTimeOffset timestamp,
        CancellationToken ct = default)
    {
        if (_config.MockMode)
        {
            await Task.Delay(50, ct).ConfigureAwait(false);
            return true;
        }

        var response = await RpcCallAsync("invokefunction", new object[]
        {
            _config.ContractHash,
            "verifyLocationAttestation",
            new object[]
            {
                new { type = "String", value = droneId },
                new { type = "Integer", value = (long)(location.Latitude * 1_000_000) },
                new { type = "Integer", value = (long)(location.Longitude * 1_000_000) },
                new { type = "Integer", value = timestamp.ToUnixTimeSeconds() }
            }
        }, ct).ConfigureAwait(false);

        return response.TryGetProperty("stack", out var stack) &&
               stack.GetArrayLength() > 0 &&
               stack[0].GetProperty("value").GetBoolean();
    }

    /// <summary>
    /// Records evidence linked to an incident on the blockchain.
    /// </summary>
    /// <param name="incidentId">The incident identifier.</param>
    /// <param name="evidenceCid">IPFS CID of the evidence.</param>
    /// <param name="evidenceType">Type of evidence.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Transaction result.</returns>
    public async Task<TransactionResult> RecordEvidenceAsync(
        string incidentId,
        string evidenceCid,
        string evidenceType,
        CancellationToken ct = default)
    {
        var evt = new BlockchainEvent
        {
            EventId = $"ev-{incidentId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            EventType = BlockchainEventType.EvidenceSubmitted,
            EvidenceCid = evidenceCid,
            Metadata = new Dictionary<string, object>
            {
                ["incident_id"] = incidentId,
                ["evidence_type"] = evidenceType
            }
        };

        return await RecordEventAsync(evt, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the status of a blockchain transaction.
    /// </summary>
    /// <param name="txHash">The transaction hash.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current transaction status.</returns>
    public async Task<TransactionStatus> GetTransactionStatusAsync(
        string txHash,
        CancellationToken ct = default)
    {
        if (_config.MockMode)
        {
            await Task.Delay(50, ct).ConfigureAwait(false);
            return TransactionStatus.Confirmed;
        }

        try
        {
            var response = await RpcCallAsync("getapplicationlog", new object[] { txHash }, ct)
                .ConfigureAwait(false);

            if (response.TryGetProperty("executions", out var executions) &&
                executions.GetArrayLength() > 0)
            {
                var state = executions[0].GetProperty("vmstate").GetString();
                return state == "HALT" ? TransactionStatus.Confirmed :
                       state == "FAULT" ? TransactionStatus.Failed :
                       TransactionStatus.Pending;
            }
        }
        catch
        {
            // Transaction not yet included in a block
        }

        return TransactionStatus.Pending;
    }

    private async Task<JsonElement> RpcCallAsync(
        string method,
        object[] parameters,
        CancellationToken ct)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = Interlocked.Increment(ref _requestId),
            method,
            @params = parameters
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("", content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("error", out var error))
        {
            var message = error.GetProperty("message").GetString();
            throw new InvalidOperationException($"Neo RPC error: {message}");
        }

        return doc.RootElement.GetProperty("result");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
