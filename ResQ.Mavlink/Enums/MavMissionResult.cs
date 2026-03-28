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
/// Result of a mission operation (MAV_MISSION_RESULT).
/// </summary>
public enum MavMissionResult : byte
{
    /// <summary>Mission accepted OK.</summary>
    Accepted = 0,

    /// <summary>Generic error / not accepting mission commands at this time.</summary>
    Error = 1,

    /// <summary>Coordinate frame is not supported.</summary>
    UnsupportedFrame = 2,

    /// <summary>Command is not supported.</summary>
    Unsupported = 3,

    /// <summary>Mission item exceeds storage space.</summary>
    NoSpace = 4,

    /// <summary>One or more mission items are invalid.</summary>
    InvalidParam = 5,

    /// <summary>Mission item received out of sequence.</summary>
    InvalidSequence = 6,

    /// <summary>Not accepting any mission commands from this communication partner.</summary>
    Denied = 7,

    /// <summary>Current mission operation cancelled (e.g. mission upload, mission download).</summary>
    InvalidData = 8,
}
