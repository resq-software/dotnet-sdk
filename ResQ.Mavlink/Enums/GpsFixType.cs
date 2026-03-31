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
/// Type of GPS fix (GPS_FIX_TYPE).
/// </summary>
public enum GpsFixType : byte
{
    /// <summary>No GPS connected.</summary>
    NoGps = 0,

    /// <summary>No position information, GPS is connected.</summary>
    NoFix = 1,

    /// <summary>2D position.</summary>
    Fix2d = 2,

    /// <summary>3D position.</summary>
    Fix3d = 3,

    /// <summary>DGPS/SBAS aided 3D position.</summary>
    Dgps = 4,

    /// <summary>RTK float, 3D position.</summary>
    RtkFloat = 5,

    /// <summary>RTK Fixed, 3D position.</summary>
    RtkFixed = 6,

    /// <summary>Static fixed, typically used for base stations.</summary>
    Static_ = 7,

    /// <summary>PPP, 3D position.</summary>
    Ppp = 8,
}
