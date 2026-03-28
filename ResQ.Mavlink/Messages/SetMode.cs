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
/// MAVLink SET_MODE message (ID 11). Set the system mode as described by the MAV_MODE enum.
/// </summary>
public readonly record struct SetMode : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 6;

    /// <summary>The new autopilot-specific mode. This field can be ignored by an autopilot.</summary>
    public uint CustomMode { get; init; }

    /// <summary>The system setting the mode.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>The new base mode.</summary>
    public MavModeFlag BaseMode { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 11;

    /// <inheritdoc/>
    public byte CrcExtra => 89;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, CustomMode);
        buffer[4] = TargetSystem;
        buffer[5] = (byte)BaseMode;
    }

    /// <summary>Deserializes a <see cref="SetMode"/> from a raw payload span.</summary>
    public static SetMode Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new SetMode
        {
            CustomMode = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            TargetSystem = buffer[4],
            BaseMode = (MavModeFlag)buffer[5],
        };
    }
}
