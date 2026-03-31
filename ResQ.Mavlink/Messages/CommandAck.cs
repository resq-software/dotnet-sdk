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
/// MAVLink COMMAND_ACK message (ID 77). Report status of a command. Includes feedback whether the specified command was executed.
/// </summary>
public readonly record struct CommandAck : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 3;

    /// <summary>Command ID (of acknowledged command).</summary>
    public MavCmd Command { get; init; }

    /// <summary>Result of command.</summary>
    public MavResult Result { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 77;

    /// <inheritdoc/>
    public byte CrcExtra => 143;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)Command);
        buffer[2] = (byte)Result;
    }

    /// <summary>Deserializes a <see cref="CommandAck"/> from a raw payload span.</summary>
    public static CommandAck Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new CommandAck
        {
            Command = (MavCmd)BinaryPrimitives.ReadUInt16LittleEndian(buffer),
            Result = (MavResult)buffer[2],
        };
    }
}
