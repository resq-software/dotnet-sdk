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
/// MAVLink SERVO_OUTPUT_RAW message (ID 36). The RAW values of the servo outputs (for quadrotors: in the range of 1000-2000).
/// </summary>
public readonly record struct ServoOutputRaw : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 21;

    /// <summary>Timestamp in microseconds.</summary>
    public uint TimeUsec { get; init; }

    /// <summary>Servo output port (set of 8 outputs = 1 port). Most MAVs only have one port.</summary>
    public byte Port { get; init; }

    /// <summary>Servo output 1 value.</summary>
    public ushort Servo1Raw { get; init; }

    /// <summary>Servo output 2 value.</summary>
    public ushort Servo2Raw { get; init; }

    /// <summary>Servo output 3 value.</summary>
    public ushort Servo3Raw { get; init; }

    /// <summary>Servo output 4 value.</summary>
    public ushort Servo4Raw { get; init; }

    /// <summary>Servo output 5 value.</summary>
    public ushort Servo5Raw { get; init; }

    /// <summary>Servo output 6 value.</summary>
    public ushort Servo6Raw { get; init; }

    /// <summary>Servo output 7 value.</summary>
    public ushort Servo7Raw { get; init; }

    /// <summary>Servo output 8 value.</summary>
    public ushort Servo8Raw { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 36;

    /// <inheritdoc/>
    public byte CrcExtra => 222;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeUsec);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..], Servo1Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[6..], Servo2Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[8..], Servo3Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[10..], Servo4Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[12..], Servo5Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[14..], Servo6Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[16..], Servo7Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[18..], Servo8Raw);
        buffer[20] = Port;
    }

    /// <summary>Deserializes a <see cref="ServoOutputRaw"/> from a raw payload span.</summary>
    public static ServoOutputRaw Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUsec = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Servo1Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[4..]),
            Servo2Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[6..]),
            Servo3Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[8..]),
            Servo4Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[10..]),
            Servo5Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[12..]),
            Servo6Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[14..]),
            Servo7Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[16..]),
            Servo8Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[18..]),
            Port = buffer[20],
        };
}
