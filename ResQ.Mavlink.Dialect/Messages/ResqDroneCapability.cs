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
using ResQ.Mavlink.Messages;

namespace ResQ.Mavlink.Dialect.Messages;

/// <summary>
/// RESQ_DRONE_CAPABILITY (ID 60006). Advertises a drone's hardware capabilities and current payload.
/// CRC extra: 44 — derived from RESQ_DRONE_CAPABILITY field layout hash.
/// Layout (12 bytes): SensorFlags(2) MaxFlightTimeMin(2) MaxSpeedMs(2) MaxPayloadGrams(2) CurrentPayloadGrams(2)
///   SystemId(1) DialectVersion(1).
/// <para>
/// Sensor flags bitfield: 0x01=RGB, 0x02=Thermal, 0x04=LiDAR, 0x08=Speaker, 0x10=DropMech, 0x20=Spotlight.
/// </para>
/// </summary>
public readonly record struct ResqDroneCapability : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 12;

    /// <inheritdoc/>
    public uint MessageId => 60006;

    /// <inheritdoc/>
    public byte CrcExtra => 44;

    /// <summary>
    /// Sensor flags bitfield. Bit 0=RGB, 1=Thermal, 2=LiDAR, 3=Speaker, 4=DropMech, 5=Spotlight.
    /// </summary>
    public ushort SensorFlags { get; init; }

    /// <summary>Maximum flight time in minutes.</summary>
    public ushort MaxFlightTimeMin { get; init; }

    /// <summary>Maximum speed in cm/s.</summary>
    public ushort MaxSpeedMs { get; init; }

    /// <summary>Maximum payload capacity in grams.</summary>
    public ushort MaxPayloadGrams { get; init; }

    /// <summary>Currently carried payload in grams.</summary>
    public ushort CurrentPayloadGrams { get; init; }

    /// <summary>MAVLink system ID of this drone.</summary>
    public byte SystemId { get; init; }

    /// <summary>ResQ dialect version implemented by this drone (currently 1).</summary>
    public byte DialectVersion { get; init; }

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, SensorFlags);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], MaxFlightTimeMin);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..], MaxSpeedMs);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[6..], MaxPayloadGrams);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[8..], CurrentPayloadGrams);
        buffer[10] = SystemId;
        buffer[11] = DialectVersion;
    }

    /// <summary>Deserializes a <see cref="ResqDroneCapability"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="ResqDroneCapability"/>.</returns>
    public static ResqDroneCapability Deserialize(ReadOnlySpan<byte> buffer) => new()
    {
        SensorFlags = BinaryPrimitives.ReadUInt16LittleEndian(buffer),
        MaxFlightTimeMin = BinaryPrimitives.ReadUInt16LittleEndian(buffer[2..]),
        MaxSpeedMs = BinaryPrimitives.ReadUInt16LittleEndian(buffer[4..]),
        MaxPayloadGrams = BinaryPrimitives.ReadUInt16LittleEndian(buffer[6..]),
        CurrentPayloadGrams = BinaryPrimitives.ReadUInt16LittleEndian(buffer[8..]),
        SystemId = buffer[10],
        DialectVersion = buffer[11],
    };
}
