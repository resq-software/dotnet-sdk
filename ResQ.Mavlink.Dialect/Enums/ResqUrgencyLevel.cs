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

namespace ResQ.Mavlink.Dialect.Enums;

/// <summary>
/// Urgency level of a distress beacon reported via <see cref="Messages.ResqEmergencyBeacon"/>.
/// </summary>
public enum ResqUrgencyLevel : byte
{
    /// <summary>Low urgency — situation stable, no immediate action required.</summary>
    Low = 0,

    /// <summary>Medium urgency — assistance needed but not immediately critical.</summary>
    Medium = 1,

    /// <summary>High urgency — immediate assistance required.</summary>
    High = 2,

    /// <summary>Life-threatening urgency — critical emergency, immediate response essential.</summary>
    LifeThreatening = 3,
}
