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
/// Classification of a detected object or hazard reported in <see cref="Messages.ResqDetection"/>.
/// </summary>
public enum ResqDetectionType : byte
{
    /// <summary>Detection type not determined.</summary>
    Unknown = 0,

    /// <summary>A person detected on the ground or in water.</summary>
    Person = 1,

    /// <summary>A vehicle (car, boat, aircraft) detected.</summary>
    Vehicle = 2,

    /// <summary>Active fire detected.</summary>
    Fire = 3,

    /// <summary>Flood or water ingress detected.</summary>
    Flood = 4,

    /// <summary>Debris field detected.</summary>
    Debris = 5,
}
