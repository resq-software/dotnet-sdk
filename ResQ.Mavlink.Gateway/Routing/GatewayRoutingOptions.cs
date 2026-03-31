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

namespace ResQ.Mavlink.Gateway.Routing;

/// <summary>
/// Configuration options for <see cref="GatewayRouter"/> message-forwarding behaviour.
/// </summary>
/// <remarks>
/// Bind this class via <c>IOptions&lt;GatewayRoutingOptions&gt;</c> in your DI container.
/// </remarks>
public sealed class GatewayRoutingOptions
{
    /// <summary>
    /// Maximum number of telemetry messages forwarded to ResQ per vehicle per second.
    /// </summary>
    /// <remarks>
    /// The rate limiter uses a sliding-window algorithm: it counts how many messages
    /// were forwarded for a given vehicle in the past one second and rejects any
    /// that would exceed this cap. Defaults to <c>10</c> Hz.
    /// </remarks>
    public int TelemetryRateLimitHz { get; set; } = 10;

    /// <summary>
    /// Set of MAVLink message IDs that must never be forwarded to the ResQ backend.
    /// </summary>
    /// <remarks>
    /// Heartbeat (ID 0) is included by default because it is a link-management
    /// message of no value to ResQ consumers.
    /// </remarks>
    public HashSet<uint> InternalOnlyMessageIds { get; set; } = [0]; // Heartbeat

    /// <summary>
    /// When <see langword="true"/> (the default), messages whose IDs are not
    /// explicitly listed in <see cref="InternalOnlyMessageIds"/> are forwarded.
    /// Set to <see langword="false"/> to implement an allow-list model where only
    /// known message types are passed through.
    /// </summary>
    public bool ForwardUnknownMessages { get; set; } = true;
}
