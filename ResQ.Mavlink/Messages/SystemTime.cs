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

using System.Buffers.Binary;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink SYSTEM_TIME message (ID 2). The system time is the time of the master clock, normally the GPS receiver.
/// </summary>
public readonly record struct SystemTime : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 12;

    /// <summary>Timestamp of the master clock in Unix epoch in microseconds.</summary>
    public ulong TimeUnixUsec { get; init; }

    /// <summary>Timestamp of the component clock since boot time in milliseconds.</summary>
    public uint TimeBootMs { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 2;

    /// <inheritdoc/>
    public byte CrcExtra => 137;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUnixUsec);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], TimeBootMs);
    }

    /// <summary>Deserializes a <see cref="SystemTime"/> from a raw payload span.</summary>
    public static SystemTime Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUnixUsec = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer[8..]),
        };
}
