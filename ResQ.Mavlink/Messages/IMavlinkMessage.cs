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

namespace ResQ.Mavlink.Messages;

/// <summary>
/// Contract for a strongly-typed MAVLink message that can serialize/deserialize its payload.
/// </summary>
public interface IMavlinkMessage
{
    /// <summary>Gets the MAVLink message ID.</summary>
    uint MessageId { get; }

    /// <summary>Gets the CRC extra seed for this message type.</summary>
    byte CrcExtra { get; }

    /// <summary>Serializes this message into <paramref name="buffer"/>.</summary>
    void Serialize(Span<byte> buffer);
}
