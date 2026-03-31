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
/// Severity level of a hazard zone reported in <see cref="Messages.ResqHazardZone"/>.
/// </summary>
public enum ResqHazardSeverity : byte
{
    /// <summary>Low severity hazard.</summary>
    Low = 0,

    /// <summary>Medium severity hazard.</summary>
    Medium = 1,

    /// <summary>High severity hazard.</summary>
    High = 2,

    /// <summary>Extreme severity hazard requiring immediate response.</summary>
    Extreme = 3,
}
