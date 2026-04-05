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
/// Represents the result of a file upload operation to IPFS.
/// </summary>
/// <remarks>
/// This record contains all relevant information about an uploaded file,
/// including its Content Identifier (CID), metadata, and pin status.
/// The CID is the permanent address of the file on the IPFS network.
/// </remarks>
/// <param name="Cid">The IPFS Content Identifier (CID) for the uploaded file.</param>
/// <param name="FileName">The original filename of the uploaded content.</param>
/// <param name="SizeBytes">The size of the file in bytes.</param>
/// <param name="ContentType">The MIME type of the file (e.g., "image/jpeg", "video/mp4").</param>
/// <param name="IsPinned">True if the file has been pinned to ensure persistence.</param>
/// <param name="Timestamp">UTC timestamp when the upload completed.</param>
/// <example>
/// <code>
/// var result = await storageClient.UploadAsync(stream, "evidence.jpg", "image/jpeg");
/// Console.WriteLine($"Uploaded to: {result.Cid}");
/// Console.WriteLine($"Size: {result.SizeBytes} bytes");
/// Console.WriteLine($"URL: {storageClient.GetGatewayUrl(result.Cid)}");
/// </code>
/// </example>
public record UploadResult(
    string Cid,
    string FileName,
    long SizeBytes,
    string ContentType,
    bool IsPinned,
    DateTimeOffset Timestamp
);

/// <summary>
/// Represents metadata for a file pinned to IPFS.
/// </summary>
/// <remarks>
/// Pin metadata provides information about files that have been pinned to IPFS
/// through the Pinata service. This includes the CID, custom metadata key-value
/// pairs, and pinning information. Pinned files are guaranteed to remain available
/// on the IPFS network as long as they remain pinned.
/// </remarks>
/// <param name="Cid">The IPFS Content Identifier.</param>
/// <param name="Name">The human-readable name assigned to the pin.</param>
/// <param name="SizeBytes">The size of the pinned content in bytes.</param>
/// <param name="PinnedAt">UTC timestamp when the content was pinned.</param>
/// <param name="KeyValues">Dictionary of custom metadata key-value pairs.</param>
/// <example>
/// <code>
/// var pins = await storageClient.ListPinsAsync("evidence-");
/// foreach (var pin in pins)
/// {
///     Console.WriteLine($"{pin.Name}: {pin.Cid}");
///     foreach (var kv in pin.KeyValues)
///     {
///         Console.WriteLine($"  {kv.Key}: {kv.Value}");
///     }
/// }
/// </code>
/// </example>
public record PinMetadata(
    string Cid,
    string Name,
    long SizeBytes,
    DateTimeOffset PinnedAt,
    Dictionary<string, string> KeyValues
);

/// <summary>
/// Interface for IPFS storage operations via Pinata.
/// </summary>
/// <remarks>
/// This interface defines the contract for storing and retrieving files on IPFS
/// through the Pinata pinning service. Implementations handle the HTTP communication
/// with Pinata's API, authentication, and error handling.
/// 
/// <para>
/// Files uploaded through this interface are automatically pinned to ensure they
/// remain available on the IPFS network. The CID returned can be used to retrieve
/// the file from any IPFS gateway.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Dependency injection registration
/// services.AddHttpClient&lt;IStorageClient, PinataClient&gt;();
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
///     public async Task&lt;string&gt; StoreEvidenceAsync(byte[] imageData)
///     {
///         var result = await _storage.UploadAsync(
///             imageData,
///             "evidence.jpg",
///             "image/jpeg",
///             new Dictionary&lt;string, string&gt;
///             {
///                 ["incidentId"] = "inc-001",
///                 ["droneId"] = "drn-001"
///             });
///         return result.Cid;
///     }
/// }
/// </code>
/// </example>
public interface IStorageClient
{
    /// <summary>
    /// Uploads a file stream to IPFS and pins it.
    /// </summary>
    /// <param name="content">The file content as a stream. The stream will be read to completion.</param>
    /// <param name="fileName">The desired filename for the uploaded content.</param>
    /// <param name="contentType">The MIME type of the content (e.g., "image/jpeg", "application/pdf").</param>
    /// <param name="metadata">Optional dictionary of custom metadata key-value pairs to attach to the pin.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// An <see cref="UploadResult"/> containing the CID and upload metadata.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when content, fileName, or contentType is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the upload fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// The stream will be read and the content uploaded to IPFS via Pinata.
    /// Once uploaded, the content is automatically pinned to ensure persistence.
    /// The returned CID can be used to retrieve the content from any IPFS gateway.
    /// </remarks>
    /// <example>
    /// <code>
    /// using var stream = File.OpenRead("photo.jpg");
    /// var result = await storage.UploadAsync(
    ///     stream,
    ///     "photo.jpg",
    ///     "image/jpeg",
    ///     new Dictionary&lt;string, string&gt; { ["source"] = "drone-001" });
    /// Console.WriteLine($"Uploaded: {result.Cid}");
    /// </code>
    /// </example>
    Task<UploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads binary data to IPFS and pins it.
    /// </summary>
    /// <param name="data">The file content as a byte array.</param>
    /// <param name="fileName">The desired filename for the uploaded content.</param>
    /// <param name="contentType">The MIME type of the content.</param>
    /// <param name="metadata">Optional dictionary of custom metadata key-value pairs.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// An <see cref="UploadResult"/> containing the CID and upload metadata.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when data, fileName, or contentType is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the upload fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// This is a convenience overload that wraps the byte array in a MemoryStream
    /// and calls <see cref="UploadAsync(Stream, string, string, Dictionary{string, string}?, CancellationToken)"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// byte[] imageBytes = await File.ReadAllBytesAsync("photo.jpg");
    /// var result = await storage.UploadAsync(
    ///     imageBytes,
    ///     "photo.jpg",
    ///     "image/jpeg");
    /// </code>
    /// </example>
    Task<UploadResult> UploadAsync(
        byte[] data,
        string fileName,
        string contentType,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves file content by its IPFS CID.
    /// </summary>
    /// <param name="cid">The IPFS Content Identifier of the file to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A stream containing the file content.</returns>
    /// <exception cref="ArgumentNullException">Thrown when cid is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file cannot be retrieved.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// The content is retrieved through the configured IPFS gateway. The caller is responsible
    /// for disposing the returned stream. In mock mode, returns a mock stream.
    /// </remarks>
    /// <example>
    /// <code>
    /// using var stream = await storage.GetAsync("Qmabc123...");
    /// using var fileStream = File.Create("downloaded.jpg");
    /// await stream.CopyToAsync(fileStream);
    /// </code>
    /// </example>
    Task<Stream> GetAsync(
        string cid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a CID is currently pinned.
    /// </summary>
    /// <param name="cid">The IPFS Content Identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the CID is pinned; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when cid is null or empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// A pinned CID is guaranteed to remain available on the IPFS network.
    /// Unpinned content may be garbage collected by IPFS nodes.
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isPinned = await storage.IsPinnedAsync("Qmabc123...");
    /// if (!isPinned)
    /// {
    ///     Console.WriteLine("Warning: Content may not be persisted");
    /// }
    /// </code>
    /// </example>
    Task<bool> IsPinnedAsync(
        string cid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpins a file from Pinata.
    /// </summary>
    /// <param name="cid">The IPFS Content Identifier to unpin.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the file was successfully unpinned; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when cid is null or empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// Unpinning removes the file from your Pinata account but does not remove it from
    /// the IPFS network. The content may still be available if other nodes have pinned it
    /// or if it has been cached. Use this to manage storage costs by removing unneeded pins.
    /// </remarks>
    /// <example>
    /// <code>
    /// bool success = await storage.UnpinAsync("Qmabc123...");
    /// if (success)
    /// {
    ///     Console.WriteLine("File unpinned successfully");
    /// }
    /// </code>
    /// </example>
    Task<bool> UnpinAsync(
        string cid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists pinned files with optional name prefix filtering.
    /// </summary>
    /// <param name="namePrefix">Optional prefix to filter pins by name. If null or empty, returns all pins.</param>
    /// <param name="limit">Maximum number of results to return (default 100).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A read-only list of pin metadata matching the filter criteria.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when limit is less than 1.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <remarks>
    /// This method queries the Pinata API for pinned content. Results include metadata
    /// about each pin including CID, name, size, and custom key-value pairs.
    /// </remarks>
    /// <example>
    /// <code>
    /// // List all pins
    /// var allPins = await storage.ListPinsAsync();
    /// 
    /// // List pins with specific prefix
    /// var evidencePins = await storage.ListPinsAsync("evidence-", limit: 50);
    /// foreach (var pin in evidencePins)
    /// {
    ///     Console.WriteLine($"{pin.Name}: {pin.Cid}");
    /// }
    /// </code>
    /// </example>
    Task<IReadOnlyList<PinMetadata>> ListPinsAsync(
        string? namePrefix = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the gateway URL for accessing content by CID.
    /// </summary>
    /// <param name="cid">The IPFS Content Identifier.</param>
    /// <returns>The full URL to access the content through the configured gateway.</returns>
    /// <exception cref="ArgumentNullException">Thrown when cid is null or empty.</exception>
    /// <remarks>
    /// The returned URL can be used in web browsers or HTTP clients to retrieve the content.
    /// The gateway URL is constructed from the configured <see cref="PinataOptions.GatewayUrl"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var gatewayUrl = storage.GetGatewayUrl("Qmabc123...");
    /// Console.WriteLine($"Access at: {gatewayUrl}");
    /// // Output: https://gateway.pinata.cloud/ipfs/Qmabc123...
    /// </code>
    /// </example>
    string GetGatewayUrl(string cid);
}
