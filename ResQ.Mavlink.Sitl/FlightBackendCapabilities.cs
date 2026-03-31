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
/// Flags describing the optional capabilities supported by an <see cref="IFlightBackend"/> implementation.
/// </summary>
[Flags]
public enum FlightBackendCapabilities
{
    /// <summary>No optional capabilities.</summary>
    None = 0,

    /// <summary>Backend provides GPS position data.</summary>
    Gps = 1 << 0,

    /// <summary>Backend supports wind disturbance injection.</summary>
    WindInjection = 1 << 1,

    /// <summary>Backend provides high-fidelity aerodynamic modelling.</summary>
    HighFidelityAerodynamics = 1 << 2,

    /// <summary>Backend supports real-time parameter tuning.</summary>
    ParameterTuning = 1 << 3,
}
