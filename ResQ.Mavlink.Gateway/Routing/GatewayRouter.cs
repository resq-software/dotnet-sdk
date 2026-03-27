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
using Microsoft.Extensions.Options;

namespace ResQ.Mavlink.Gateway.Routing;

/// <summary>
/// Decides whether an incoming MAVLink message should be forwarded to the ResQ backend,
/// applying both an internal-message filter and a per-vehicle telemetry rate limit.
/// </summary>
/// <remarks>
/// <para>
/// The router is entirely stateless with respect to message content — it only tracks
/// forwarding timestamps. The caller is responsible for calling <see cref="RecordForwarded"/>
/// after a message has actually been dispatched.
/// </para>
/// <para>
/// Rate limiting uses a sliding window: the router retains the timestamps of the last
/// <see cref="GatewayRoutingOptions.TelemetryRateLimitHz"/> forwarded messages per vehicle
/// and rejects the next message if the oldest timestamp in the window is within the past second.
/// </para>
/// </remarks>
public sealed class GatewayRouter
{
    private readonly GatewayRoutingOptions _options;

    // Per-vehicle sliding window of forwarding timestamps.
    private readonly ConcurrentDictionary<byte, Queue<DateTimeOffset>> _windows = new();

    /// <summary>
    /// Initialises a new <see cref="GatewayRouter"/> with the supplied options.
    /// </summary>
    /// <param name="options">Routing configuration (rate limit, internal-only IDs, etc.).</param>
    public GatewayRouter(IOptions<GatewayRoutingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <summary>
    /// Determines whether a message from <paramref name="systemId"/> with
    /// <paramref name="messageId"/> should be forwarded to ResQ.
    /// </summary>
    /// <param name="systemId">The MAVLink system ID of the sending vehicle.</param>
    /// <param name="messageId">The MAVLink message type identifier.</param>
    /// <returns>
    /// <see langword="true"/> if the message should be forwarded;
    /// <see langword="false"/> if it is filtered (internal-only) or rate-limited.
    /// </returns>
    /// <remarks>
    /// This method does <em>not</em> record the forwarding event. Call
    /// <see cref="RecordForwarded"/> after actually sending the message.
    /// </remarks>
    public bool ShouldForwardToResq(byte systemId, uint messageId)
    {
        // 1. Internal-only filter.
        if (_options.InternalOnlyMessageIds.Contains(messageId))
        {
            return false;
        }

        // 2. Rate-limit check (sliding window over the last 1 second).
        var window = _windows.GetOrAdd(systemId, _ => new Queue<DateTimeOffset>());
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-1);

        lock (window)
        {
            // Evict entries older than 1 second.
            while (window.Count > 0 && window.Peek() <= cutoff)
            {
                window.Dequeue();
            }

            if (window.Count >= _options.TelemetryRateLimitHz)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Records that a message from <paramref name="systemId"/> has been forwarded,
    /// advancing the sliding-window counter for that vehicle.
    /// </summary>
    /// <param name="systemId">The MAVLink system ID of the vehicle whose message was forwarded.</param>
    /// <remarks>
    /// Call this method immediately after dispatching a message that passed
    /// <see cref="ShouldForwardToResq"/>. It is safe to call from multiple threads.
    /// </remarks>
    public void RecordForwarded(byte systemId)
    {
        var window = _windows.GetOrAdd(systemId, _ => new Queue<DateTimeOffset>());

        lock (window)
        {
            window.Enqueue(DateTimeOffset.UtcNow);
        }
    }
}
