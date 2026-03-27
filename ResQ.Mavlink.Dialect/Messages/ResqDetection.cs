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
/// RESQ_DETECTION (ID 60000). Reports a detected object or hazard with bounding box and location.
/// CRC extra: 142 — derived from RESQ_DETECTION field layout hash.
/// Layout (30 bytes): TimestampMs(8) LatE7(4) LonE7(4) AltMm(4) BboxX(2) BboxY(2) BboxW(2) BboxH(2) DetectionType(1) Confidence(1).
/// Fields ordered largest-type-first per MAVLink wire-order convention.
/// </summary>
public readonly record struct ResqDetection : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 30;

    /// <inheritdoc/>
    public uint MessageId => 60000;

    /// <inheritdoc/>
    public byte CrcExtra => 142;

    /// <summary>Unix timestamp in milliseconds.</summary>
    public ulong TimestampMs { get; init; }

    /// <summary>Detection latitude in degE7.</summary>
    public int LatE7 { get; init; }

    /// <summary>Detection longitude in degE7.</summary>
    public int LonE7 { get; init; }

    /// <summary>Altitude in millimetres (MSL).</summary>
    public int AltMm { get; init; }

    /// <summary>Bounding box top-left X in pixels.</summary>
    public ushort BboxX { get; init; }

    /// <summary>Bounding box top-left Y in pixels.</summary>
    public ushort BboxY { get; init; }

    /// <summary>Bounding box width in pixels.</summary>
    public ushort BboxW { get; init; }

    /// <summary>Bounding box height in pixels.</summary>
    public ushort BboxH { get; init; }

    /// <summary>Type of detected object.</summary>
    public ResqDetectionType DetectionType { get; init; }

    /// <summary>Confidence level 0–100 percent.</summary>
    public byte Confidence { get; init; }

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimestampMs);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], LatE7);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], LonE7);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], AltMm);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[20..], BboxX);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[22..], BboxY);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[24..], BboxW);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[26..], BboxH);
        buffer[28] = (byte)DetectionType;
        buffer[29] = Confidence;
    }

    /// <summary>Deserializes a <see cref="ResqDetection"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="ResqDetection"/>.</returns>
    public static ResqDetection Deserialize(ReadOnlySpan<byte> buffer) => new()
    {
        TimestampMs = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
        LatE7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
        LonE7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
        AltMm = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]),
        BboxX = BinaryPrimitives.ReadUInt16LittleEndian(buffer[20..]),
        BboxY = BinaryPrimitives.ReadUInt16LittleEndian(buffer[22..]),
        BboxW = BinaryPrimitives.ReadUInt16LittleEndian(buffer[24..]),
        BboxH = BinaryPrimitives.ReadUInt16LittleEndian(buffer[26..]),
        DetectionType = (ResqDetectionType)buffer[28],
        Confidence = buffer[29],
    };
}
