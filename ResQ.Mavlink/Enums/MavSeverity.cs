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
/// Indicates the severity level, generally used for status messages to indicate their relative urgency.
/// Based on RFC-5424 using syslog values (MAV_SEVERITY).
/// </summary>
public enum MavSeverity : byte
{
    /// <summary>System is unusable. This is a "panic" condition.</summary>
    Emergency = 0,

    /// <summary>Action should be taken immediately. Indicates error in non-critical systems.</summary>
    Alert = 1,

    /// <summary>Action must be taken immediately. Indicates failure in a primary system.</summary>
    Critical = 2,

    /// <summary>Indicates an error in secondary/redundant systems.</summary>
    Error = 3,

    /// <summary>Indicates about a possible future error if this is not resolved within a given timeframe. Example would be a low battery warning.</summary>
    Warning = 4,

    /// <summary>An unusual event has occurred, though not an error condition. This should be investigated for the root cause.</summary>
    Notice = 5,

    /// <summary>Normal operational messages. Useful for logging. No action is required for these messages.</summary>
    Info = 6,

    /// <summary>Useful non-operational messages that can assist in debugging.</summary>
    Debug = 7,
}
