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
/// MAVLink CAMERA_IMAGE_CAPTURED message (ID 263). Information about a captured image.
/// </summary>
public readonly record struct CameraImageCaptured : IMavlinkMessage
{
    /// <summary>Payload size in bytes (simplified).</summary>
    public const int PayloadSize = 36;

    /// <summary>Timestamp (time since UNIX epoch) in us.</summary>
    public ulong TimeUtc { get; init; }

    /// <summary>Timestamp (time since system boot) in ms.</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>Latitude of image in degrees * 1e7.</summary>
    public int Lat { get; init; }

    /// <summary>Longitude of image in degrees * 1e7.</summary>
    public int Lon { get; init; }

    /// <summary>Altitude (MSL) of image in mm.</summary>
    public int Alt { get; init; }

    /// <summary>Altitude above ground of image in mm.</summary>
    public int RelativeAlt { get; init; }

    /// <summary>Image index.</summary>
    public int ImageIndex { get; init; }

    /// <summary>Camera ID (1 for first, 2 for second, etc.).</summary>
    public byte CameraId { get; init; }

    /// <summary>Boolean indicating success (1) or failure (0) while capturing this image.</summary>
    public sbyte CaptureResult { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 263;

    /// <inheritdoc/>
    public byte CrcExtra => 133;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUtc);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], TimeBootMs);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], Lat);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], Lon);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[20..], Alt);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[24..], RelativeAlt);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[28..], ImageIndex);
        buffer[32] = CameraId;
        buffer[33] = (byte)CaptureResult;
        // bytes 34-35 spare
    }

    /// <summary>Deserializes a <see cref="CameraImageCaptured"/> from a raw payload span.</summary>
    public static CameraImageCaptured Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUtc = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer[8..]),
            Lat = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
            Lon = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]),
            Alt = BinaryPrimitives.ReadInt32LittleEndian(buffer[20..]),
            RelativeAlt = BinaryPrimitives.ReadInt32LittleEndian(buffer[24..]),
            ImageIndex = BinaryPrimitives.ReadInt32LittleEndian(buffer[28..]),
            CameraId = buffer[32],
            CaptureResult = (sbyte)buffer[33],
        };
}
