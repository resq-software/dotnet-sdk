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
/// Nature of the hazard reported in <see cref="Messages.ResqHazardZone"/>.
/// </summary>
public enum ResqHazardType : byte
{
    /// <summary>Active fire hazard zone.</summary>
    Fire = 0,

    /// <summary>Flood or water hazard zone.</summary>
    Flood = 1,

    /// <summary>High-wind hazard zone.</summary>
    Wind = 2,

    /// <summary>Toxic chemical or gas hazard zone.</summary>
    Toxic = 3,

    /// <summary>Structural collapse or instability hazard zone.</summary>
    Structural = 4,
}
