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
/// MAVLink BATTERY_STATUS message (ID 147). Battery information.
/// </summary>
public readonly record struct BatteryStatus : IMavlinkMessage
{
    /// <summary>Payload size in bytes (simplified — omits per-cell voltages array).</summary>
    public const int PayloadSize = 36;

    /// <summary>Battery ID.</summary>
    public byte Id { get; init; }

    /// <summary>Function of the battery.</summary>
    public byte BatteryFunction { get; init; }

    /// <summary>Type (chemistry) of the battery.</summary>
    public byte Type { get; init; }

    /// <summary>Temperature of the battery in cdegC. INT16_MAX for unknown.</summary>
    public short Temperature { get; init; }

    /// <summary>Battery voltage of cell 1 (1-10) in mV (0: cell does not exist).</summary>
    public ushort Voltages0 { get; init; }

    /// <summary>Battery current in 10 * mA, -1 if autopilot does not measure the current.</summary>
    public short CurrentBattery { get; init; }

    /// <summary>Consumed charge in mAh, -1 if autopilot does not provide this.</summary>
    public int CurrentConsumed { get; init; }

    /// <summary>Consumed energy in hJ, -1 if autopilot does not provide this.</summary>
    public int EnergyConsumed { get; init; }

    /// <summary>Remaining battery energy: 0-100% (0: unexpectedly empty, 100: full, -1: autopilot does not estimate this).</summary>
    public sbyte BatteryRemaining { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 147;

    /// <inheritdoc/>
    public byte CrcExtra => 154;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, CurrentConsumed);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], EnergyConsumed);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[8..], Temperature);
        // 10 cell voltage slots (2 bytes each = 20 bytes) - fill first one, rest zeros
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[10..], Voltages0);
        // bytes 12-29 are remaining voltage slots (zero)
        BinaryPrimitives.WriteInt16LittleEndian(buffer[30..], CurrentBattery);
        buffer[32] = Id;
        buffer[33] = BatteryFunction;
        buffer[34] = Type;
        buffer[35] = (byte)BatteryRemaining;
    }

    /// <summary>Deserializes a <see cref="BatteryStatus"/> from a raw payload span.</summary>
    public static BatteryStatus Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            CurrentConsumed = BinaryPrimitives.ReadInt32LittleEndian(buffer),
            EnergyConsumed = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
            Temperature = BinaryPrimitives.ReadInt16LittleEndian(buffer[8..]),
            Voltages0 = BinaryPrimitives.ReadUInt16LittleEndian(buffer[10..]),
            CurrentBattery = BinaryPrimitives.ReadInt16LittleEndian(buffer[30..]),
            Id = buffer[32],
            BatteryFunction = buffer[33],
            Type = buffer[34],
            BatteryRemaining = (sbyte)buffer[35],
        };
}
