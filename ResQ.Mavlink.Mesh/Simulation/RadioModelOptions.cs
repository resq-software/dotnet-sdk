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

namespace ResQ.Mavlink.Mesh.Simulation;

/// <summary>
/// Configuration options for <see cref="RadioModel"/>.
/// </summary>
public sealed class RadioModelOptions
{
    /// <summary>Maximum communication range in metres.</summary>
    public float MaxRangeMetres { get; set; } = 500f;

    /// <summary>Signal attenuation exponent (free-space path loss exponent).</summary>
    public float AttenuationFactor { get; set; } = 2.0f;

    /// <summary>Minimum baseline packet loss percentage at zero distance.</summary>
    public float BasePacketLossPercent { get; set; } = 1.0f;
}
