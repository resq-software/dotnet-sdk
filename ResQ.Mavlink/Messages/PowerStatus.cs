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
/// MAVLink POWER_STATUS message (ID 125). Power supply status.
/// </summary>
public readonly record struct PowerStatus : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 6;

    /// <summary>5V rail voltage in mV.</summary>
    public ushort Vcc { get; init; }

    /// <summary>Servo rail voltage in mV.</summary>
    public ushort Vservo { get; init; }

    /// <summary>Bitmap of power supply status flags.</summary>
    public ushort Flags { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 125;

    /// <inheritdoc/>
    public byte CrcExtra => 203;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Vcc);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], Vservo);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..], Flags);
    }

    /// <summary>Deserializes a <see cref="PowerStatus"/> from a raw payload span.</summary>
    public static PowerStatus Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            Vcc = BinaryPrimitives.ReadUInt16LittleEndian(buffer),
            Vservo = BinaryPrimitives.ReadUInt16LittleEndian(buffer[2..]),
            Flags = BinaryPrimitives.ReadUInt16LittleEndian(buffer[4..]),
        };
}
