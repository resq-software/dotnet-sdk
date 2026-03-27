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

namespace ResQ.Mavlink.Mesh;

/// <summary>
/// Configuration options for <see cref="MeshRelay"/>.
/// </summary>
public sealed class MeshRelayOptions
{
    /// <summary>
    /// Maximum number of packets buffered during a partition event.
    /// </summary>
    public int MaxBufferSize { get; set; } = 1000;

    /// <summary>
    /// When <see langword="true"/>, the lowest-priority buffered message is evicted when the
    /// buffer is full and a higher-priority message arrives.
    /// When <see langword="false"/>, new messages are dropped if the buffer is full.
    /// </summary>
    public bool PriorityEviction { get; set; } = true;
}
