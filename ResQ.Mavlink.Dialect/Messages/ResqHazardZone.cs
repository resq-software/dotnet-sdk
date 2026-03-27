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
using ResQ.Mavlink.Dialect.Enums;
using ResQ.Mavlink.Messages;

namespace ResQ.Mavlink.Dialect.Messages;

/// <summary>
/// RESQ_HAZARD_ZONE (ID 60004). Defines a circular hazard zone with progression vector.
/// CRC extra: 188 — derived from RESQ_HAZARD_ZONE field layout hash.
/// Layout (34 bytes): TimestampMs(8) ZoneId(4) CenterLatE7(4) CenterLonE7(4) RadiusMetres(4)
///   ProgressionSpeed(4) ProgressionHeading(4) HazardType(1) Severity(1).
/// </summary>
public readonly record struct ResqHazardZone : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 34;

    /// <inheritdoc/>
    public uint MessageId => 60004;

    /// <inheritdoc/>
    public byte CrcExtra => 188;

    /// <summary>Unix timestamp in milliseconds.</summary>
    public ulong TimestampMs { get; init; }

    /// <summary>Unique zone identifier.</summary>
    public uint ZoneId { get; init; }

    /// <summary>Zone centre latitude in degE7.</summary>
    public int CenterLatE7 { get; init; }

    /// <summary>Zone centre longitude in degE7.</summary>
    public int CenterLonE7 { get; init; }

    /// <summary>Zone radius in metres.</summary>
    public uint RadiusMetres { get; init; }

    /// <summary>Hazard expansion speed in m/s.</summary>
    public float ProgressionSpeed { get; init; }

    /// <summary>Hazard expansion heading in radians.</summary>
    public float ProgressionHeading { get; init; }

    /// <summary>Type of hazard.</summary>
    public ResqHazardType HazardType { get; init; }

    /// <summary>Hazard severity: 0=Low, 1=Medium, 2=High, 3=Extreme.</summary>
    public byte Severity { get; init; }

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimestampMs);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], ZoneId);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], CenterLatE7);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], CenterLonE7);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[20..], RadiusMetres);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], ProgressionSpeed);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], ProgressionHeading);
        buffer[32] = (byte)HazardType;
        buffer[33] = Severity;
    }

    /// <summary>Deserializes a <see cref="ResqHazardZone"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="ResqHazardZone"/>.</returns>
    public static ResqHazardZone Deserialize(ReadOnlySpan<byte> buffer) => new()
    {
        TimestampMs = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
        ZoneId = BinaryPrimitives.ReadUInt32LittleEndian(buffer[8..]),
        CenterLatE7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
        CenterLonE7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]),
        RadiusMetres = BinaryPrimitives.ReadUInt32LittleEndian(buffer[20..]),
        ProgressionSpeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
        ProgressionHeading = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
        HazardType = (ResqHazardType)buffer[32],
        Severity = buffer[33],
    };
}
