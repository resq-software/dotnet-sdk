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
/// Enumeration of system states (MAV_STATE).
/// </summary>
public enum MavState : byte
{
    /// <summary>Uninitialized system, state is unknown.</summary>
    Uninit = 0,

    /// <summary>System is booting up.</summary>
    Boot = 1,

    /// <summary>System is calibrating and not flight-ready.</summary>
    Calibrating = 2,

    /// <summary>System is grounded and on standby. It can be launched any time.</summary>
    Standby = 3,

    /// <summary>System is active and might be already airborne. Motors are engaged.</summary>
    Active = 4,

    /// <summary>System is in a non-normal flight mode (for example, attempting to return to home).</summary>
    Critical = 5,

    /// <summary>System is in a non-normal flight mode (that is, it is trying to survive).</summary>
    Emergency = 6,

    /// <summary>System just initialized its power-down sequence, will shut down now.</summary>
    Poweroff = 7,

    /// <summary>System is terminating itself (i.e. hard emergency stop) or finalizing a failed mission.</summary>
    FlightTermination = 8,
}
