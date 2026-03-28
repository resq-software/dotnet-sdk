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
/// Search pattern type used in <see cref="Messages.ResqSwarmTask"/>.
/// </summary>
public enum ResqSearchPattern : byte
{
    /// <summary>Parallel lawnmower search pattern.</summary>
    Parallel = 0,

    /// <summary>Inward or outward spiral search pattern.</summary>
    Spiral = 1,

    /// <summary>Expanding box search pattern.</summary>
    Expanding = 2,
}
