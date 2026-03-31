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

namespace ResQ.Mavlink.Connection;

/// <summary>
/// Configuration options for <see cref="MavlinkConnection"/>.
/// </summary>
public sealed class MavlinkConnectionOptions
{
    /// <summary>Gets or sets the MAVLink system ID for this GCS node. Defaults to 255.</summary>
    public byte SystemId { get; set; } = 255;

    /// <summary>Gets or sets the MAVLink component ID for this GCS node. Defaults to 190.</summary>
    public byte ComponentId { get; set; } = 190;

    /// <summary>Gets or sets how often a HEARTBEAT is sent. Defaults to 1 second.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets or sets how long to wait for a COMMAND_ACK before timing out. Defaults to 1500 ms.</summary>
    public TimeSpan CommandAckTimeout { get; set; } = TimeSpan.FromMilliseconds(1500);

    /// <summary>Gets or sets how many times a command will be retried before failing. Defaults to 3.</summary>
    public int CommandRetryCount { get; set; } = 3;
}
