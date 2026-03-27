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
/// MAVLink ACTUATOR_CONTROL_TARGET message (ID 140). Set the vehicle attitude and body angular rates.
/// </summary>
public readonly record struct ActuatorControlTarget : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 41;

    /// <summary>Timestamp in microseconds.</summary>
    public ulong TimeUsec { get; init; }

    /// <summary>Actuator control values (normalized -1..1, 8 controls).</summary>
    public float Controls0 { get; init; }

    /// <summary>Control 1.</summary>
    public float Controls1 { get; init; }

    /// <summary>Control 2.</summary>
    public float Controls2 { get; init; }

    /// <summary>Control 3.</summary>
    public float Controls3 { get; init; }

    /// <summary>Control 4.</summary>
    public float Controls4 { get; init; }

    /// <summary>Control 5.</summary>
    public float Controls5 { get; init; }

    /// <summary>Control 6.</summary>
    public float Controls6 { get; init; }

    /// <summary>Control 7.</summary>
    public float Controls7 { get; init; }

    /// <summary>Actuator group. The "_mlx" indicates this is a multi-instance message and a MAVLink target is indexed by 1..3</summary>
    public byte GroupMlx { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 140;

    /// <inheritdoc/>
    public byte CrcExtra => 181;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUsec);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Controls0);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Controls1);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Controls2);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], Controls3);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Controls4);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], Controls5);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[32..], Controls6);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[36..], Controls7);
        buffer[40] = GroupMlx;
    }

    /// <summary>Deserializes a <see cref="ActuatorControlTarget"/> from a raw payload span.</summary>
    public static ActuatorControlTarget Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUsec = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            Controls0 = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Controls1 = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Controls2 = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Controls3 = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            Controls4 = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            Controls5 = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
            Controls6 = BinaryPrimitives.ReadSingleLittleEndian(buffer[32..]),
            Controls7 = BinaryPrimitives.ReadSingleLittleEndian(buffer[36..]),
            GroupMlx = buffer[40],
        };
}
