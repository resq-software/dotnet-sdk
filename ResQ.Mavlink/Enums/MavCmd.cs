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
/// Commands to be executed by the MAV. They can be executed on request from an external agent
/// with the target system / target component being 0 (broadcast) or other specific system
/// (MAV_CMD subset used in Phase 1).
/// </summary>
public enum MavCmd : ushort
{
    /// <summary>Navigate to waypoint.</summary>
    NavWaypoint = 16,

    /// <summary>Loiter around this waypoint an unlimited number of times.</summary>
    NavLoiterUnlim = 17,

    /// <summary>Navigate to the return-to-launch point.</summary>
    NavReturnToLaunch = 20,

    /// <summary>Land at location.</summary>
    NavLand = 21,

    /// <summary>Takeoff from ground / hand.</summary>
    NavTakeoff = 22,

    /// <summary>Set system mode.</summary>
    DoSetMode = 176,

    /// <summary>Arms / Disarms a component.</summary>
    ComponentArmDisarm = 400,

    /// <summary>Mission start — start the current mission from sequence number.</summary>
    MissionStart = 300,
}
