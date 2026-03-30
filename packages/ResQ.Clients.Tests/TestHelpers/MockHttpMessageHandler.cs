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

using System.Net;

namespace ResQ.Clients.Tests.TestHelpers;

/// <summary>
/// Mock HttpMessageHandler for deterministic HTTP client testing.
/// </summary>
/// <remarks>
/// This allows us to test retry logic, circuit breaker behavior, and error handling
/// without making actual HTTP requests. Provides full control over response status codes,
/// content, and delays.
/// </remarks>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly Queue<Exception> _exceptions = new();
    private readonly List<HttpRequestMessage> _requests = new();

    /// <summary>
    /// All HTTP requests that were sent through this handler.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

    /// <summary>
    /// Queues a successful response to be returned for the next request.
    /// </summary>
    public void QueueResponse(HttpStatusCode statusCode, string content = "")
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        });
    }

    /// <summary>
    /// Queues a successful JSON response to be returned for the next request.
    /// </summary>
    public void QueueJsonResponse(HttpStatusCode statusCode, string json)
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
    }

    /// <summary>
    /// Queues an exception to be thrown for the next request.
    /// </summary>
    public void QueueException(Exception exception)
    {
        _exceptions.Enqueue(exception);
    }

    /// <summary>
    /// Queues a network timeout exception for the next request.
    /// </summary>
    public void QueueTimeout()
    {
        _exceptions.Enqueue(new TaskCanceledException("Request timed out"));
    }

    /// <summary>
    /// Queues a network error exception for the next request.
    /// </summary>
    public void QueueNetworkError()
    {
        _exceptions.Enqueue(new HttpRequestException("Network error"));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Store request for verification
        _requests.Add(request);

        // Throw exception if queued
        if (_exceptions.TryDequeue(out var exception))
        {
            throw exception;
        }

        // Return response if queued
        if (_responses.TryDequeue(out var response))
        {
            return Task.FromResult(response);
        }

        // Default: 404 Not Found if nothing queued
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("No response queued")
        });
    }

    /// <summary>
    /// Resets the handler state (clears all queued responses and requests).
    /// </summary>
    public void Reset()
    {
        _responses.Clear();
        _exceptions.Clear();
        _requests.Clear();
    }
}
