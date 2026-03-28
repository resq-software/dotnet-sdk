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
/// MAVLink SYS_STATUS message (ID 1). General system state information.
/// </summary>
public readonly record struct SysStatus : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 31;

    /// <summary>Bitmap showing which onboard controllers and sensors are present.</summary>
    public uint OnboardControlSensorsPresent { get; init; }

    /// <summary>Bitmap showing which onboard controllers and sensors are enabled.</summary>
    public uint OnboardControlSensorsEnabled { get; init; }

    /// <summary>Bitmap showing which onboard controllers and sensors are operational or have an error.</summary>
    public uint OnboardControlSensorsHealth { get; init; }

    /// <summary>Maximum usage in percent of the mainloop time.</summary>
    public ushort Load { get; init; }

    /// <summary>Battery voltage, in millivolts (1 = 1 millivolt).</summary>
    public ushort VoltageBattery { get; init; }

    /// <summary>Battery current, in 10*milliamperes (1 = 10 milliampere), -1: autopilot does not measure the current.</summary>
    public short CurrentBattery { get; init; }

    /// <summary>Communication drops in percent, (0%: 0, 100%: 10'000).</summary>
    public ushort DropRateComm { get; init; }

    /// <summary>Communication errors (UART, I2C, SPI, CAN), dropped packets on all links.</summary>
    public ushort ErrorsComm { get; init; }

    /// <summary>Autopilot-specific errors.</summary>
    public ushort ErrorsCount1 { get; init; }

    /// <summary>Autopilot-specific errors.</summary>
    public ushort ErrorsCount2 { get; init; }

    /// <summary>Autopilot-specific errors.</summary>
    public ushort ErrorsCount3 { get; init; }

    /// <summary>Autopilot-specific errors.</summary>
    public ushort ErrorsCount4 { get; init; }

    /// <summary>Remaining battery energy, -1: autopilot estimate the remaining battery.</summary>
    public sbyte BatteryRemaining { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 1;

    /// <inheritdoc/>
    public byte CrcExtra => 124;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, OnboardControlSensorsPresent);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[4..], OnboardControlSensorsEnabled);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], OnboardControlSensorsHealth);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[12..], Load);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[14..], VoltageBattery);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[16..], CurrentBattery);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[18..], DropRateComm);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[20..], ErrorsComm);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[22..], ErrorsCount1);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[24..], ErrorsCount2);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[26..], ErrorsCount3);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[28..], ErrorsCount4);
        buffer[30] = (byte)BatteryRemaining;
    }

    /// <summary>Deserializes a <see cref="SysStatus"/> from a raw payload span.</summary>
    public static SysStatus Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new SysStatus
        {
            OnboardControlSensorsPresent = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            OnboardControlSensorsEnabled = BinaryPrimitives.ReadUInt32LittleEndian(buffer[4..]),
            OnboardControlSensorsHealth = BinaryPrimitives.ReadUInt32LittleEndian(buffer[8..]),
            Load = BinaryPrimitives.ReadUInt16LittleEndian(buffer[12..]),
            VoltageBattery = BinaryPrimitives.ReadUInt16LittleEndian(buffer[14..]),
            CurrentBattery = BinaryPrimitives.ReadInt16LittleEndian(buffer[16..]),
            DropRateComm = BinaryPrimitives.ReadUInt16LittleEndian(buffer[18..]),
            ErrorsComm = BinaryPrimitives.ReadUInt16LittleEndian(buffer[20..]),
            ErrorsCount1 = BinaryPrimitives.ReadUInt16LittleEndian(buffer[22..]),
            ErrorsCount2 = BinaryPrimitives.ReadUInt16LittleEndian(buffer[24..]),
            ErrorsCount3 = BinaryPrimitives.ReadUInt16LittleEndian(buffer[26..]),
            ErrorsCount4 = BinaryPrimitives.ReadUInt16LittleEndian(buffer[28..]),
            BatteryRemaining = (sbyte)buffer[30],
        };
    }
}
