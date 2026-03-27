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

namespace ResQ.Mavlink.Gateway.Gcs;

/// <summary>
/// Configuration options for <see cref="GcsPassthrough"/>.
/// </summary>
/// <remarks>
/// Bind this class via <c>IOptions&lt;GcsPassthroughOptions&gt;</c> in your DI container.
/// </remarks>
public sealed class GcsPassthroughOptions
{
    /// <summary>
    /// Gets or sets the local UDP port on which to accept GCS connections. Defaults to 14551.
    /// </summary>
    public int GcsListenPort { get; set; } = 14551;

    /// <summary>
    /// Gets or sets whether ResQ command priority override is enabled. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, GCS command packets (CommandLong, SetMode, SetPositionTargetGlobalInt)
    /// are silently dropped for 2 seconds after a ResQ command has been issued via
    /// <see cref="GcsPassthrough.NotifyResqCommand"/>.
    /// </remarks>
    public bool ResqPriorityOverride { get; set; } = true;

    /// <summary>
    /// Gets or sets whether GCS passthrough is active. Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
