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

namespace ResQ.Mavlink.Sitl;

/// <summary>
/// Configuration options for <see cref="ArduPilotSitlBackend"/>.
/// </summary>
public sealed class SitlBackendOptions
{
    /// <summary>
    /// Gets or sets the zero-based SITL instance index used for port allocation.
    /// Defaults to <c>0</c>.
    /// </summary>
    public int InstanceIndex { get; set; } = 0;

    /// <summary>
    /// Gets or sets the physics stepping rate in Hz. Defaults to <c>400</c>.
    /// </summary>
    public int PhysicsRateHz { get; set; } = 400;

    /// <summary>
    /// Gets or sets the ArduPilot vehicle type string (e.g. <c>"ArduCopter"</c>).
    /// Defaults to <c>"ArduCopter"</c>.
    /// </summary>
    public string VehicleType { get; set; } = "ArduCopter";
}
