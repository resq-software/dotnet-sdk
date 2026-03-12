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

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ResQ.Storage;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text;
using Xunit;

namespace ResQ.Storage.Tests;

/// <summary>
/// Unit tests for the <see cref="PinataClient"/> implementation.
/// </summary>
public class PinataClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly PinataOptions _options;
    private readonly Mock<ILogger<PinataClient>> _mockLogger;
    private readonly HttpClient _httpClient;

    public PinataClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();

        _options = new PinataOptions
        {
            JwtToken = "test_jwt_token",
            MockMode = false,
            GatewayUrl = "https://gateway.pinata.cloud",
            MaxFileSizeBytes = 100 * 1024 * 1024
        };

        _mockLogger = new Mock<ILogger<PinataClient>>();
    }

    [Fact]
    public async Task UploadAsync_WithValidStream_ShouldReturnSuccessResult()
    {
        // Arrange
        var responseJson = """
        {
            "IpfsHash": "QmTest123456789",
            "PinSize": 1024,
            "Timestamp": "2026-02-12T12:00:00Z"
        }
        """;

        _mockHttp
            .When(HttpMethod.Post, "*/pinning/pinFileToIPFS")
            .Respond("application/json", responseJson);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test data"));

        // Act
        var result = await client.UploadAsync(stream, "test.txt", "text/plain");

        // Assert
        result.Should().NotBeNull();
        result.Cid.Should().Be("QmTest123456789");
        result.FileName.Should().Be("test.txt");
        result.ContentType.Should().Be("text/plain");
        result.IsPinned.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_WithByteArray_ShouldReturnSuccessResult()
    {
        // Arrange
        var responseJson = """
        {
            "IpfsHash": "QmByteArrayTest",
            "PinSize": 512,
            "Timestamp": "2026-02-12T12:00:00Z"
        }
        """;

        _mockHttp
            .When(HttpMethod.Post, "*/pinning/pinFileToIPFS")
            .Respond("application/json", responseJson);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);
        var data = Encoding.UTF8.GetBytes("test data");

        // Act
        var result = await client.UploadAsync(data, "test.txt", "text/plain");

        // Assert
        result.Should().NotBeNull();
        result.Cid.Should().Be("QmByteArrayTest");
        result.SizeBytes.Should().Be(512); // Size from API response
    }

    [Fact]
    public async Task UploadAsync_WithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        var responseJson = """
        {
            "IpfsHash": "QmMetadataTest",
            "PinSize": 1024,
            "Timestamp": "2026-02-12T12:00:00Z"
        }
        """;

        _mockHttp
            .When(HttpMethod.Post, "*/pinning/pinFileToIPFS")
            .Respond("application/json", responseJson);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        var metadata = new Dictionary<string, string>
        {
            ["incidentId"] = "inc-001",
            ["droneId"] = "drn-001"
        };

        // Act
        var result = await client.UploadAsync(stream, "test.txt", "text/plain", metadata);

        // Assert
        result.Should().NotBeNull();
        result.Cid.Should().Be("QmMetadataTest");
    }

    [Fact]
    public async Task UploadAsync_WithNetworkError_ShouldThrowException()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Post, "*/pinning/pinFileToIPFS")
            .Respond(HttpStatusCode.InternalServerError);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await client.UploadAsync(stream, "test.txt", "text/plain")
        );
    }

    [Fact]
    public async Task GetAsync_WithValidCid_ShouldReturnStream()
    {
        // Arrange
        var testContent = "test file content";
        _mockHttp
            .When(HttpMethod.Get, "*/ipfs/*")
            .Respond("text/plain", testContent);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);

        // Act
        var stream = await client.GetAsync("QmTest123");

        // Assert
        stream.Should().NotBeNull();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        content.Should().Be(testContent);
    }

    [Fact]
    public async Task IsPinnedAsync_WithPinnedCid_ShouldReturnTrue()
    {
        // Arrange
        var responseJson = """
        {
            "count": 1,
            "rows": [
                {
                    "ipfs_pin_hash": "QmTest123",
                    "size": 1024,
                    "date_pinned": "2026-02-12T12:00:00Z"
                }
            ]
        }
        """;

        _mockHttp
            .When(HttpMethod.Get, "*/data/pinList*")
            .Respond("application/json", responseJson);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);

        // Act
        var isPinned = await client.IsPinnedAsync("QmTest123");

        // Assert
        isPinned.Should().BeTrue();
    }

    [Fact]
    public async Task IsPinnedAsync_WithUnpinnedCid_ShouldReturnFalse()
    {
        // Arrange
        var responseJson = """
        {
            "count": 0,
            "rows": []
        }
        """;

        _mockHttp
            .When(HttpMethod.Get, "*/data/pinList*")
            .Respond("application/json", responseJson);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);

        // Act
        var isPinned = await client.IsPinnedAsync("QmNotPinned");

        // Assert
        isPinned.Should().BeFalse();
    }

    [Fact]
    public async Task UnpinAsync_WithValidCid_ShouldReturnTrue()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Delete, "*/pinning/unpin/*")
            .Respond(HttpStatusCode.OK);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);

        // Act
        var success = await client.UnpinAsync("QmTest123");

        // Assert
        success.Should().BeTrue();
    }

    [Fact]
    public async Task ListPinsAsync_WithNoFilter_ShouldReturnAllPins()
    {
        // Arrange
        var responseJson = """
        {
            "count": 2,
            "rows": [
                {
                    "ipfs_pin_hash": "QmPin1",
                    "size": 1024,
                    "date_pinned": "2026-02-12T12:00:00Z",
                    "metadata": {
                        "name": "file1.jpg",
                        "keyvalues": {
                            "incidentId": "inc-001"
                        }
                    }
                },
                {
                    "ipfs_pin_hash": "QmPin2",
                    "size": 2048,
                    "date_pinned": "2026-02-12T13:00:00Z",
                    "metadata": {
                        "name": "file2.jpg",
                        "keyvalues": {
                            "incidentId": "inc-002"
                        }
                    }
                }
            ]
        }
        """;

        _mockHttp
            .When(HttpMethod.Get, "*/data/pinList*")
            .Respond("application/json", responseJson);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);

        // Act
        var pins = await client.ListPinsAsync();

        // Assert
        pins.Should().HaveCount(2);
        pins[0].Cid.Should().Be("QmPin1");
        pins[1].Cid.Should().Be("QmPin2");
    }

    [Fact]
    public async Task ListPinsAsync_WithPrefix_ShouldFilterByPrefix()
    {
        // Arrange
        var responseJson = """
        {
            "count": 1,
            "rows": [
                {
                    "ipfs_pin_hash": "QmEvidence1",
                    "size": 1024,
                    "date_pinned": "2026-02-12T12:00:00Z",
                    "metadata": {
                        "name": "evidence-001.jpg",
                        "keyvalues": {}
                    }
                }
            ]
        }
        """;

        _mockHttp
            .When(HttpMethod.Get, "*/data/pinList*")
            .Respond("application/json", responseJson);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);

        // Act
        var pins = await client.ListPinsAsync("evidence-");

        // Assert
        pins.Should().ContainSingle();
        pins[0].Name.Should().StartWith("evidence-");
    }

    [Fact]
    public void GetGatewayUrl_WithValidCid_ShouldReturnCorrectUrl()
    {
        // Arrange
        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);
        var cid = "QmTest123";

        // Act
        var url = client.GetGatewayUrl(cid);

        // Assert
        url.Should().Be($"https://gateway.pinata.cloud/ipfs/{cid}");
    }

    [Fact]
    public async Task UploadAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await client.UploadAsync(stream, "test.txt", "text/plain", null, cts.Token)
        );
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("video/mp4")]
    [InlineData("application/json")]
    [InlineData("text/plain")]
    public async Task UploadAsync_WithDifferentContentTypes_ShouldSucceed(string contentType)
    {
        // Arrange
        var responseJson = """
        {
            "IpfsHash": "QmContentTypeTest",
            "PinSize": 1024,
            "Timestamp": "2026-02-12T12:00:00Z"
        }
        """;

        _mockHttp
            .When(HttpMethod.Post, "*/pinning/pinFileToIPFS")
            .Respond("application/json", responseJson);

        var client = new PinataClient(_httpClient, Options.Create(_options), _mockLogger.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

        // Act
        var result = await client.UploadAsync(stream, "test.file", contentType);

        // Assert
        result.ContentType.Should().Be(contentType);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
    }
}
