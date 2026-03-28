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
using ResQ.Mavlink.Enums;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink COMMAND_LONG message (ID 76). Send a command with up to seven parameters to the MAV.
/// </summary>
public readonly record struct CommandLong : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 33;

    /// <summary>Parameter 1 (for the specific command).</summary>
    public float Param1 { get; init; }

    /// <summary>Parameter 2 (for the specific command).</summary>
    public float Param2 { get; init; }

    /// <summary>Parameter 3 (for the specific command).</summary>
    public float Param3 { get; init; }

    /// <summary>Parameter 4 (for the specific command).</summary>
    public float Param4 { get; init; }

    /// <summary>Parameter 5 (for the specific command).</summary>
    public float Param5 { get; init; }

    /// <summary>Parameter 6 (for the specific command).</summary>
    public float Param6 { get; init; }

    /// <summary>Parameter 7 (for the specific command).</summary>
    public float Param7 { get; init; }

    /// <summary>Command ID (of command to send).</summary>
    public MavCmd Command { get; init; }

    /// <summary>System which should execute the command.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>Component which should execute the command, 0 for all components.</summary>
    public byte TargetComponent { get; init; }

    /// <summary>0: First transmission of this command. 1-255: Confirmation transmissions (e.g. for kill command).</summary>
    public byte Confirmation { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 76;

    /// <inheritdoc/>
    public byte CrcExtra => 152;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteSingleLittleEndian(buffer, Param1);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Param2);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Param3);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Param4);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Param5);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], Param6);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Param7);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[28..], (ushort)Command);
        buffer[30] = TargetSystem;
        buffer[31] = TargetComponent;
        buffer[32] = Confirmation;
    }

    /// <summary>Deserializes a <see cref="CommandLong"/> from a raw payload span.</summary>
    public static CommandLong Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new CommandLong
        {
            Param1 = BinaryPrimitives.ReadSingleLittleEndian(buffer),
            Param2 = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            Param3 = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Param4 = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Param5 = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Param6 = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            Param7 = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            Command = (MavCmd)BinaryPrimitives.ReadUInt16LittleEndian(buffer[28..]),
            TargetSystem = buffer[30],
            TargetComponent = buffer[31],
            Confirmation = buffer[32],
        };
    }
}
