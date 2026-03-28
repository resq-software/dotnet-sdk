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
/// MAVLink SCALED_PRESSURE message (ID 29). Barometric pressure and temperature.
/// </summary>
public readonly record struct ScaledPressure : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 14;

    /// <summary>Timestamp (time since system boot) in ms.</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>Absolute pressure in hPa.</summary>
    public float PressAbs { get; init; }

    /// <summary>Differential pressure 1 in hPa.</summary>
    public float PressDiff { get; init; }

    /// <summary>Absolute pressure temperature in cdegC.</summary>
    public short Temperature { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 29;

    /// <inheritdoc/>
    public byte CrcExtra => 115;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], PressAbs);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], PressDiff);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[12..], Temperature);
    }

    /// <summary>Deserializes a <see cref="ScaledPressure"/> from a raw payload span.</summary>
    public static ScaledPressure Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            PressAbs = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            PressDiff = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Temperature = BinaryPrimitives.ReadInt16LittleEndian(buffer[12..]),
        };
}
