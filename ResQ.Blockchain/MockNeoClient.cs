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

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ResQ.Blockchain;

/// <summary>
/// Mock implementation of <see cref="INeoClient"/> for testing and development.
/// </summary>
/// <remarks>
/// This mock client provides an in-memory implementation of the Neo N3 blockchain interface,
/// suitable for unit testing and local development without requiring a real blockchain connection.
/// All operations succeed immediately and generate deterministic fake transaction hashes.
///
/// <para>
/// Events are stored in memory and can be retrieved by incident ID. Block height increments
/// with each transaction. No actual blockchain transactions are performed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration with dependency injection
/// services.AddSingleton&lt;INeoClient, MockNeoClient&gt;();
///
/// // Direct usage
/// var mockClient = new MockNeoClient(logger);
/// var result = await mockClient.RecordEventAsync(evt);
/// Console.WriteLine($"Mock TX: {result.TransactionHash}");
/// </code>
/// </example>
public class MockNeoClient : INeoClient
{
    private readonly ILogger<MockNeoClient> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<BlockchainEvent>> _eventsByIncident = new();
    private long _blockHeight = 1000000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockNeoClient"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for recording mock operations.</param>
    /// <remarks>
    /// The mock client starts with a default block height of 1,000,000 and maintains
    /// an empty event store that gets populated as events are recorded.
    /// </remarks>
    /// <example>
    /// <code>
    /// var logger = loggerFactory.CreateLogger&lt;MockNeoClient&gt;();
    /// var client = new MockNeoClient(logger);
    /// </code>
    /// </example>
    public MockNeoClient(ILogger<MockNeoClient> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a mock blockchain event in memory.
    /// </summary>
    /// <param name="evt">The blockchain event to record.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="TransactionResult"/> with a generated transaction hash and confirmed status.
    /// </returns>
    /// <remarks>
    /// This mock implementation:
    /// <list type="bullet">
    /// <item>Generates a random transaction hash using cryptographic random bytes</item>
    /// <item>Increments the internal block height</item>
    /// <item>Stores the event in memory, indexed by incident ID if present in payload</item>
    /// <item>Logs the operation at Information level</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var evt = new BlockchainEvent(
    ///     EventId: "evt-001",
    ///     EventType: "IncidentDetected",
    ///     Payload: "{\"incident\":\"inc-001\"}",
    ///     IpfsCid: null,
    ///     Timestamp: DateTimeOffset.UtcNow
    /// );
    ///
    /// var result = await mockClient.RecordEventAsync(evt);
    /// // Logs: "MOCK: Recorded event evt-001 of type IncidentDetected, TxHash: 0x..."
    /// </code>
    /// </example>
    public Task<TransactionResult> RecordEventAsync(
        BlockchainEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt, nameof(evt));
        cancellationToken.ThrowIfCancellationRequested();

        var txHash = GenerateTxHash();
        var blockHeight = (ulong)Interlocked.Increment(ref _blockHeight);

        _logger.LogInformation(
            "MOCK: Recorded event {EventId} of type {EventType}, TxHash: {TxHash}",
            evt.EventId, evt.EventType, txHash);

        // Index by incident ID extracted from payload, falling back to EventId
        var incidentId = ExtractIncidentId(evt.Payload) ?? evt.EventId;
        _eventsByIncident.GetOrAdd(incidentId, _ => new ConcurrentBag<BlockchainEvent>()).Add(evt);

        return Task.FromResult(new TransactionResult(
            txHash,
            IsConfirmed: true,
            blockHeight,
            DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Records a mock location attestation in memory.
    /// </summary>
    /// <param name="attestation">The location attestation to record.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="TransactionResult"/> with a generated transaction hash and confirmed status.
    /// </returns>
    /// <remarks>
    /// This mock implementation generates a transaction hash and increments the block height,
    /// logging the attestation details including drone ID and coordinates.
    /// </remarks>
    /// <example>
    /// <code>
    /// var attestation = new LocationAttestation(
    ///     DroneId: "drn-001",
    ///     Latitude: 37.7749,
    ///     Longitude: -122.4194,
    ///     Altitude: 100.0,
    ///     Timestamp: DateTimeOffset.UtcNow,
    ///     Signature: "0x..."
    /// );
    ///
    /// var result = await mockClient.RecordLocationAttestationAsync(attestation);
    /// // Logs: "MOCK: Recorded location attestation for drn-001 at (37.7749, -122.4194), TxHash: 0x..."
    /// </code>
    /// </example>
    public Task<TransactionResult> RecordLocationAttestationAsync(
        LocationAttestation attestation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attestation);
        cancellationToken.ThrowIfCancellationRequested();

        if (attestation.Latitude < -90.0 || attestation.Latitude > 90.0)
            throw new ArgumentOutOfRangeException(
                nameof(attestation.Latitude),
                attestation.Latitude,
                "Latitude must be between -90 and 90 degrees");

        if (attestation.Longitude < -180.0 || attestation.Longitude > 180.0)
            throw new ArgumentOutOfRangeException(
                nameof(attestation.Longitude),
                attestation.Longitude,
                "Longitude must be between -180 and 180 degrees");

        var txHash = GenerateTxHash();
        var blockHeight = (ulong)Interlocked.Increment(ref _blockHeight);

        _logger.LogInformation(
            "MOCK: Recorded location attestation for {DroneId} at ({Lat}, {Lon}), TxHash: {TxHash}",
            attestation.DroneId, attestation.Latitude, attestation.Longitude, txHash);

        return Task.FromResult(new TransactionResult(
            txHash,
            IsConfirmed: true,
            blockHeight,
            DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Verifies a location attestation in mock mode.
    /// </summary>
    /// <param name="attestation">The location attestation to verify.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// True if the attestation has a non-empty signature; false otherwise.
    /// </returns>
    /// <remarks>
    /// In mock mode, verification simply checks that a signature is present.
    /// No actual cryptographic verification is performed. This allows testing
    /// of both valid and invalid attestation scenarios by providing or omitting
    /// the signature field.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Valid attestation (has signature)
    /// var valid = new LocationAttestation(..., Signature: "0xabc...");
    /// var isValid = await mockClient.VerifyLocationAttestationAsync(valid);
    /// // Returns: true
    ///
    /// // Invalid attestation (no signature)
    /// var invalid = new LocationAttestation(..., Signature: "");
    /// var isInvalid = await mockClient.VerifyLocationAttestationAsync(invalid);
    /// // Returns: false
    /// </code>
    /// </example>
    public Task<bool> VerifyLocationAttestationAsync(
        LocationAttestation attestation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attestation);
        cancellationToken.ThrowIfCancellationRequested();

        if (attestation.Latitude < -90.0 || attestation.Latitude > 90.0)
            throw new ArgumentOutOfRangeException(
                nameof(attestation.Latitude),
                attestation.Latitude,
                "Latitude must be between -90 and 90 degrees");

        if (attestation.Longitude < -180.0 || attestation.Longitude > 180.0)
            throw new ArgumentOutOfRangeException(
                nameof(attestation.Longitude),
                attestation.Longitude,
                "Longitude must be between -180 and 180 degrees");

        // In mock mode, always verify successfully if signature is present
        var isValid = !string.IsNullOrEmpty(attestation.Signature);

        _logger.LogInformation(
            "MOCK: Verified attestation for {DroneId}: {IsValid}",
            attestation.DroneId, isValid);

        return Task.FromResult(isValid);
    }

    /// <summary>
    /// Records mock evidence metadata in memory.
    /// </summary>
    /// <param name="evidence">The evidence record to record.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="TransactionResult"/> with a generated transaction hash and confirmed status.
    /// </returns>
    /// <remarks>
    /// This mock implementation generates a transaction hash and logs the evidence
    /// details including the incident ID and IPFS CID.
    /// </remarks>
    /// <example>
    /// <code>
    /// var evidence = new EvidenceRecord(
    ///     IncidentId: "inc-001",
    ///     IpfsCid: "Qmabc123...",
    ///     ContentType: "image/jpeg",
    ///     SizeBytes: 1024567,
    ///     Hash: "sha256:..."
    /// );
    ///
    /// var result = await mockClient.RecordEvidenceAsync(evidence);
    /// // Logs: "MOCK: Recorded evidence for incident inc-001, CID: Qmabc123..., TxHash: 0x..."
    /// </code>
    /// </example>
    public Task<TransactionResult> RecordEvidenceAsync(
        EvidenceRecord evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence, nameof(evidence));
        cancellationToken.ThrowIfCancellationRequested();

        var txHash = GenerateTxHash();
        var blockHeight = (ulong)Interlocked.Increment(ref _blockHeight);

        _logger.LogInformation(
            "MOCK: Recorded evidence for incident {IncidentId}, CID: {Cid}, TxHash: {TxHash}",
            evidence.IncidentId, evidence.IpfsCid, txHash);

        // Index the evidence event under its incident ID so GetEventsByIncidentAsync can find it
        var evidenceEvent = new BlockchainEvent(
            EventId: txHash,
            EventType: "EvidenceRecorded",
            Payload: JsonSerializer.Serialize(new { evidence.IncidentId, evidence.IpfsCid, evidence.ContentType }),
            IpfsCid: evidence.IpfsCid,
            Timestamp: DateTimeOffset.UtcNow);
        _eventsByIncident.GetOrAdd(evidence.IncidentId, _ => new ConcurrentBag<BlockchainEvent>()).Add(evidenceEvent);

        return Task.FromResult(new TransactionResult(
            txHash,
            IsConfirmed: true,
            blockHeight,
            DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Retrieves mock events by incident ID from the in-memory store.
    /// </summary>
    /// <param name="incidentId">The incident ID to query.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A read-only list of events associated with the incident, or an empty list if none exist.
    /// </returns>
    /// <remarks>
    /// Events are stored when recorded via <see cref="RecordEventAsync"/> if the payload
    /// contains an "incident" field. This method retrieves all events that were indexed
    /// under the specified incident ID.
    /// </remarks>
    /// <example>
    /// <code>
    /// // First record some events
    /// var evt1 = new BlockchainEvent(..., Payload: "{\"incident\":\"inc-001\"}");
    /// await mockClient.RecordEventAsync(evt1);
    ///
    /// // Then retrieve them
    /// var events = await mockClient.GetEventsByIncidentAsync("inc-001");
    /// Console.WriteLine($"Found {events.Count} events");
    /// </code>
    /// </example>
    public Task<IReadOnlyList<BlockchainEvent>> GetEventsByIncidentAsync(
        string incidentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(incidentId, nameof(incidentId));
        cancellationToken.ThrowIfCancellationRequested();

        if (_eventsByIncident.TryGetValue(incidentId, out var events))
        {
            return Task.FromResult<IReadOnlyList<BlockchainEvent>>(events.ToArray());
        }

        return Task.FromResult<IReadOnlyList<BlockchainEvent>>(Array.Empty<BlockchainEvent>());
    }

    /// <summary>
    /// Gets the current mock block height.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The current block height, starting at 1,000,000 and incrementing with each transaction.</returns>
    /// <remarks>
    /// The block height starts at 1,000,000 and increases by 1 for each recorded transaction.
    /// This simulates blockchain growth without requiring an actual network connection.
    /// </remarks>
    /// <example>
    /// <code>
    /// var initialHeight = await mockClient.GetBlockHeightAsync();
    /// // Returns: 1000000
    ///
    /// await mockClient.RecordEventAsync(evt);
    ///
    /// var newHeight = await mockClient.GetBlockHeightAsync();
    /// // Returns: 1000001
    /// </code>
    /// </example>
    public Task<ulong> GetBlockHeightAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult((ulong)Interlocked.Read(ref _blockHeight));
    }

    /// <summary>
    /// Generates a random transaction hash for mock purposes.
    /// </summary>
    /// <returns>A hexadecimal string representing a 32-byte transaction hash, prefixed with "0x".</returns>
    /// <remarks>
    /// Uses <see cref="RandomNumberGenerator"/> to generate cryptographically secure random bytes,
    /// ensuring unique transaction hashes for each mock operation.
    /// </remarks>
    private static string GenerateTxHash()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return "0x" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Extracts an incident ID from a JSON payload string.
    /// </summary>
    /// <param name="payload">The JSON payload to parse.</param>
    /// <returns>The extracted incident ID from "incidentId" or "incident" fields, or null if not found.</returns>
    private static string? ExtractIncidentId(string? payload)
    {
        if (string.IsNullOrEmpty(payload)) return null;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.TryGetProperty("incidentId", out var incidentId))
                return incidentId.GetString();

            if (root.TryGetProperty("incident", out var incident))
                return incident.GetString();

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
