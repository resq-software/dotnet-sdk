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
/// These flags encode the MAV mode (MAV_MODE_FLAG). Bitmask of enabled mode flags.
/// </summary>
[Flags]
public enum MavModeFlag : byte
{
    /// <summary>0b00000001 Reserved for future use.</summary>
    CustomModeEnabled = 1,

    /// <summary>0b00000010 System is allowed to be active in a test mode.</summary>
    TestEnabled = 2,

    /// <summary>0b00000100 Autonomous mode enabled, system finds its own goal positions.</summary>
    AutoEnabled = 4,

    /// <summary>0b00001000 Guided mode enabled, system is following a path/waypoint.</summary>
    GuidedEnabled = 8,

    /// <summary>0b00010000 System is stabilized electronically by its controller.</summary>
    StabilizeEnabled = 16,

    /// <summary>0b00100000 Hardware in the loop simulation. All motors / actuators are blocked, but internal software is full operational.</summary>
    HilEnabled = 32,

    /// <summary>0b01000000 Remote control input is enabled.</summary>
    ManualEnabled = 64,

    /// <summary>0b10000000 MAV safety set to armed. Motors are enabled / running / can start. Ready to fly. Additional protection from human override is removed.</summary>
    SafetyArmed = 128,
}
