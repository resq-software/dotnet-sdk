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
/// MAVLink TIMESYNC message (ID 111). Time synchronization activity between master and slave.
/// </summary>
public readonly record struct Timesync : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 16;

    /// <summary>Time sync timestamp 1 in ns.</summary>
    public long Tc1 { get; init; }

    /// <summary>Time sync timestamp 2 in ns.</summary>
    public long Ts1 { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 111;

    /// <inheritdoc/>
    public byte CrcExtra => 34;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt64LittleEndian(buffer, Tc1);
        BinaryPrimitives.WriteInt64LittleEndian(buffer[8..], Ts1);
    }

    /// <summary>Deserializes a <see cref="Timesync"/> from a raw payload span.</summary>
    public static Timesync Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            Tc1 = BinaryPrimitives.ReadInt64LittleEndian(buffer),
            Ts1 = BinaryPrimitives.ReadInt64LittleEndian(buffer[8..]),
        };
}
