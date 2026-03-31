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
/// RESQ_EMERGENCY_BEACON (ID 60007). Mesh-relayed distress beacon with TTL hop counter.
/// CRC extra: 161 — derived from RESQ_EMERGENCY_BEACON field layout hash.
/// Layout (27 bytes): TimestampMs(8) BeaconId(4) LatE7(4) LonE7(4) AltMm(4) BeaconType(1) Urgency(1) Ttl(1).
/// Total field bytes: 8+4+4+4+4+1+1+1 = 27.
/// </summary>
public readonly record struct ResqEmergencyBeacon : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 27;

    /// <inheritdoc/>
    public uint MessageId => 60007;

    /// <inheritdoc/>
    public byte CrcExtra => 161;

    /// <summary>Unix timestamp in milliseconds.</summary>
    public ulong TimestampMs { get; init; }

    /// <summary>Unique beacon identifier.</summary>
    public uint BeaconId { get; init; }

    /// <summary>Beacon latitude in degE7.</summary>
    public int LatE7 { get; init; }

    /// <summary>Beacon longitude in degE7.</summary>
    public int LonE7 { get; init; }

    /// <summary>Altitude in millimetres.</summary>
    public int AltMm { get; init; }

    /// <summary>Type of emergency.</summary>
    public ResqBeaconType BeaconType { get; init; }

    /// <summary>Urgency level of the emergency.</summary>
    public ResqUrgencyLevel Urgency { get; init; }

    /// <summary>Remaining mesh relay hops (decrements at each relay node).</summary>
    public byte Ttl { get; init; }

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimestampMs);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], BeaconId);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], LatE7);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], LonE7);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[20..], AltMm);
        buffer[24] = (byte)BeaconType;
        buffer[25] = (byte)Urgency;
        buffer[26] = Ttl;
    }

    /// <summary>Deserializes a <see cref="ResqEmergencyBeacon"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="ResqEmergencyBeacon"/>.</returns>
    public static ResqEmergencyBeacon Deserialize(ReadOnlySpan<byte> buffer) => new()
    {
        TimestampMs = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
        BeaconId = BinaryPrimitives.ReadUInt32LittleEndian(buffer[8..]),
        LatE7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
        LonE7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]),
        AltMm = BinaryPrimitives.ReadInt32LittleEndian(buffer[20..]),
        BeaconType = (ResqBeaconType)buffer[24],
        Urgency = (ResqUrgencyLevel)buffer[25],
        Ttl = buffer[26],
    };
}
