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

namespace ResQ.Simulation.Engine.Environment;

/// <summary>
/// Classifies the surface material at a terrain coordinate, used by flight models
/// and mission planners to adjust behaviour (e.g. landing suitability).
/// </summary>
public enum SurfaceType
{
    /// <summary>
    /// Ground covered by grass, shrubs, trees, or other plant matter.
    /// </summary>
    Vegetation,

    /// <summary>
    /// Open water: lakes, rivers, flooded terrain, or coastal areas.
    /// </summary>
    Water,

    /// <summary>
    /// Built-up area including roads, rooftops, and paved surfaces.
    /// </summary>
    Urban,

    /// <summary>
    /// Exposed soil, rock, sand, or gravel with no significant vegetation or structure.
    /// </summary>
    BareGround,
}
