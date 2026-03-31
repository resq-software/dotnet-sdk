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
/// Micro air vehicle / autopilot class (MAV_AUTOPILOT).
/// </summary>
public enum MavAutopilot : byte
{
    /// <summary>Generic autopilot, full support for everything.</summary>
    Generic = 0,

    /// <summary>Reserved for future use.</summary>
    Reserved = 1,

    /// <summary>SLUGS autopilot.</summary>
    Slugs = 2,

    /// <summary>ArduPilot — Ardupilot. Plane/Copter/Rover/Sub/Tracker autopilot.</summary>
    ArduPilotMega = 3,

    /// <summary>PX4 Autopilot — https://docs.px4.io/main/en/</summary>
    Px4 = 12,
}
