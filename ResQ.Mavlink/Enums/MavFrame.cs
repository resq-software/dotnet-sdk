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
/// Coordinate frames used in MAVLink (MAV_FRAME).
/// </summary>
public enum MavFrame : byte
{
    /// <summary>Global (WGS84) coordinate frame + altitude over mean sea level (MSL).</summary>
    Global = 0,

    /// <summary>NED local tangent frame (x: North, y: East, z: Down) with origin fixed relative to earth.</summary>
    LocalNed = 1,

    /// <summary>NOT a coordinate frame, indicates a mission command.</summary>
    Mission = 2,

    /// <summary>Global (WGS84) coordinate frame + altitude relative to the home position.</summary>
    GlobalRelativeAlt = 3,

    /// <summary>ENU local tangent frame (x: East, y: North, z: Up) with origin fixed relative to earth.</summary>
    LocalEnu = 4,

    /// <summary>Global (WGS84) coordinate frame (scaled) + altitude over MSL.</summary>
    GlobalInt = 5,

    /// <summary>Global (WGS84) coordinate frame (scaled) + altitude relative to the home position.</summary>
    GlobalRelativeAltInt = 6,

    /// <summary>Offset to the current local frame. Anything expressed in this frame should be added to the current local frame position.</summary>
    LocalOffsetNed = 7,

    /// <summary>Global (WGS84) coordinate frame with AGL altitude (altitude above terrain).</summary>
    GlobalTerrainAlt = 10,

    /// <summary>Global (WGS84) coordinate frame (scaled) with AGL altitude (altitude above terrain).</summary>
    GlobalTerrainAltInt = 11,
}
