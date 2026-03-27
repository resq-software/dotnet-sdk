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
/// Acknowledgement type for detection reports in <see cref="Messages.ResqDetectionAck"/>.
/// </summary>
public enum ResqDetectionAckType : byte
{
    /// <summary>Detection confirmed as valid by acknowledging drone.</summary>
    Confirmed = 0,

    /// <summary>Detection is a duplicate of an earlier report.</summary>
    Duplicate = 1,

    /// <summary>Detection is being investigated; status not yet determined.</summary>
    Investigating = 2,
}
