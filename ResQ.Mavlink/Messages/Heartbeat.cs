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
/// MAVLink HEARTBEAT message (ID 0). Sent at 1 Hz to indicate system is alive.
/// </summary>
public readonly record struct Heartbeat : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 9;

    /// <summary>A bitfield for use for autopilot-specific flags.</summary>
    public uint CustomMode { get; init; }

    /// <summary>Type of the system (vehicle type).</summary>
    public MavType Type { get; init; }

    /// <summary>Autopilot type / class.</summary>
    public MavAutopilot Autopilot { get; init; }

    /// <summary>System mode bitmap.</summary>
    public MavModeFlag BaseMode { get; init; }

    /// <summary>System status flag.</summary>
    public MavState SystemStatus { get; init; }

    /// <summary>MAVLink version — not writable by user, gets set in use.</summary>
    public byte MavlinkVersion { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 0;

    /// <inheritdoc/>
    public byte CrcExtra => 50;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, CustomMode);
        buffer[4] = (byte)Type;
        buffer[5] = (byte)Autopilot;
        buffer[6] = (byte)BaseMode;
        buffer[7] = (byte)SystemStatus;
        buffer[8] = MavlinkVersion;
    }

    /// <summary>Deserializes a <see cref="Heartbeat"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="Heartbeat"/>.</returns>
    public static Heartbeat Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new Heartbeat
        {
            CustomMode = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Type = (MavType)buffer[4],
            Autopilot = (MavAutopilot)buffer[5],
            BaseMode = (MavModeFlag)buffer[6],
            SystemStatus = (MavState)buffer[7],
            MavlinkVersion = buffer[8],
        };
    }
}
