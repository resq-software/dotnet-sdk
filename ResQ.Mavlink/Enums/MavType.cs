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
/// MAVLINK component type (MAV_TYPE). Identifies what kind of vehicle/component this is.
/// </summary>
public enum MavType : byte
{
    /// <summary>Generic micro air vehicle.</summary>
    Generic = 0,

    /// <summary>Fixed-wing aircraft.</summary>
    FixedWing = 1,

    /// <summary>Quadrotor.</summary>
    Quadrotor = 2,

    /// <summary>Coaxial helicopter.</summary>
    Coaxial = 3,

    /// <summary>Normal helicopter with tail rotor.</summary>
    Helicopter = 4,

    /// <summary>Ground control station.</summary>
    Gcs = 6,

    /// <summary>Ground rover.</summary>
    GroundRover = 10,

    /// <summary>Submarine.</summary>
    Submarine = 12,

    /// <summary>Hexarotor.</summary>
    Hexarotor = 13,

    /// <summary>Octorotor.</summary>
    Octorotor = 14,

    /// <summary>Tricopter.</summary>
    Tricopter = 15,
}
