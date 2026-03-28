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
/// MAVLink MOUNT_ORIENTATION message (ID 265). Orientation of a mount.
/// </summary>
public readonly record struct MountOrientation : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 16;

    /// <summary>Timestamp (time since system boot) in ms.</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>Roll in global frame (NaN for invalid/unknown).</summary>
    public float Roll { get; init; }

    /// <summary>Pitch in global frame (NaN for invalid/unknown).</summary>
    public float Pitch { get; init; }

    /// <summary>Yaw relative to vehicle (NaN for invalid/unknown).</summary>
    public float Yaw { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 265;

    /// <inheritdoc/>
    public byte CrcExtra => 26;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Roll);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Pitch);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Yaw);
    }

    /// <summary>Deserializes a <see cref="MountOrientation"/> from a raw payload span.</summary>
    public static MountOrientation Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Roll = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            Pitch = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Yaw = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
        };
}
