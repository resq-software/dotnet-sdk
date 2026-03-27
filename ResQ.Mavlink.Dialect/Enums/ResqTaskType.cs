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
/// Type of task assigned to a drone via <see cref="Messages.ResqSwarmTask"/>.
/// </summary>
public enum ResqTaskType : byte
{
    /// <summary>Search a defined area for targets.</summary>
    Search = 0,

    /// <summary>Conduct a systematic survey of an area.</summary>
    Survey = 1,

    /// <summary>Track a specific target continuously.</summary>
    Track = 2,

    /// <summary>Deliver a payload to a destination.</summary>
    Deliver = 3,

    /// <summary>Act as a communications relay node.</summary>
    Relay = 4,
}
