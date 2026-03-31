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
/// MAVLink VIBRATION message (ID 241). Vibration levels and accelerometer clipping.
/// </summary>
public readonly record struct Vibration : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 32;

    /// <summary>Timestamp in microseconds.</summary>
    public ulong TimeUsec { get; init; }

    /// <summary>Vibration levels on X-axis.</summary>
    public float VibrationX { get; init; }

    /// <summary>Vibration levels on Y-axis.</summary>
    public float VibrationY { get; init; }

    /// <summary>Vibration levels on Z-axis.</summary>
    public float VibrationZ { get; init; }

    /// <summary>First accelerometer clipping count.</summary>
    public uint Clipping0 { get; init; }

    /// <summary>Second accelerometer clipping count.</summary>
    public uint Clipping1 { get; init; }

    /// <summary>Third accelerometer clipping count.</summary>
    public uint Clipping2 { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 241;

    /// <inheritdoc/>
    public byte CrcExtra => 90;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUsec);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], VibrationX);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], VibrationY);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], VibrationZ);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[20..], Clipping0);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[24..], Clipping1);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[28..], Clipping2);
    }

    /// <summary>Deserializes a <see cref="Vibration"/> from a raw payload span.</summary>
    public static Vibration Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUsec = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            VibrationX = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            VibrationY = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            VibrationZ = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Clipping0 = BinaryPrimitives.ReadUInt32LittleEndian(buffer[20..]),
            Clipping1 = BinaryPrimitives.ReadUInt32LittleEndian(buffer[24..]),
            Clipping2 = BinaryPrimitives.ReadUInt32LittleEndian(buffer[28..]),
        };
}
