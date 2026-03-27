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
/// RESQ_DETECTION_ACK (ID 60001). Acknowledgement sent by a drone confirming receipt of a detection.
/// CRC extra: 73 — derived from RESQ_DETECTION_ACK field layout hash.
/// Layout (18 bytes): OriginalTimestampMs(8) LatE7(4) LonE7(4) AckType(1) AckerSystemId(1).
/// </summary>
public readonly record struct ResqDetectionAck : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 18;

    /// <inheritdoc/>
    public uint MessageId => 60001;

    /// <inheritdoc/>
    public byte CrcExtra => 73;

    /// <summary>Timestamp of the detection being acknowledged.</summary>
    public ulong OriginalTimestampMs { get; init; }

    /// <summary>Acker's latitude in degE7.</summary>
    public int LatE7 { get; init; }

    /// <summary>Acker's longitude in degE7.</summary>
    public int LonE7 { get; init; }

    /// <summary>Acknowledgement type: 0=Confirmed, 1=Duplicate, 2=Investigating.</summary>
    public byte AckType { get; init; }

    /// <summary>System ID of the acknowledging drone.</summary>
    public byte AckerSystemId { get; init; }

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, OriginalTimestampMs);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], LatE7);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], LonE7);
        buffer[16] = AckType;
        buffer[17] = AckerSystemId;
    }

    /// <summary>Deserializes a <see cref="ResqDetectionAck"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="ResqDetectionAck"/>.</returns>
    public static ResqDetectionAck Deserialize(ReadOnlySpan<byte> buffer) => new()
    {
        OriginalTimestampMs = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
        LatE7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
        LonE7 = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
        AckType = buffer[16],
        AckerSystemId = buffer[17],
    };
}
