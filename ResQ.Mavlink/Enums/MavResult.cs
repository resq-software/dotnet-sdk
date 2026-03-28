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

namespace ResQ.Mavlink.Enums;

/// <summary>
/// Result from a MAVLink command (MAV_RESULT).
/// </summary>
public enum MavResult : byte
{
    /// <summary>Command is valid (is supported and has valid parameters), and was executed.</summary>
    Accepted = 0,

    /// <summary>Command is valid, but cannot be executed at this time. Retry later.</summary>
    TemporarilyRejected = 1,

    /// <summary>Command is invalid (is supported but has invalid parameters). Retrying same command and parameters will not work.</summary>
    Denied = 2,

    /// <summary>Command is not supported (unknown).</summary>
    Unsupported = 3,

    /// <summary>Command is valid, but execution has failed.</summary>
    Failed = 4,

    /// <summary>Command is valid and is being executed. Final result will be COMMAND_ACK with MAV_RESULT_ACCEPTED once completed.</summary>
    InProgress = 5,
}
