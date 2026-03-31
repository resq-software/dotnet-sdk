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
/// MAVLink ATTITUDE_QUATERNION message (ID 61). The attitude in the aeronautical frame (right-handed, Z-down, X-front, Y-right), expressed as quaternion.
/// </summary>
public readonly record struct AttitudeQuaternion : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 32;

    /// <summary>Timestamp (time since system boot) in ms.</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>Quaternion component 1, w (1 in null-rotation).</summary>
    public float Q1 { get; init; }

    /// <summary>Quaternion component 2, x (0 in null-rotation).</summary>
    public float Q2 { get; init; }

    /// <summary>Quaternion component 3, y (0 in null-rotation).</summary>
    public float Q3 { get; init; }

    /// <summary>Quaternion component 4, z (0 in null-rotation).</summary>
    public float Q4 { get; init; }

    /// <summary>Roll angular speed in rad/s.</summary>
    public float Rollspeed { get; init; }

    /// <summary>Pitch angular speed in rad/s.</summary>
    public float Pitchspeed { get; init; }

    /// <summary>Yaw angular speed in rad/s.</summary>
    public float Yawspeed { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 61;

    /// <inheritdoc/>
    public byte CrcExtra => 246;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Q1);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Q2);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Q3);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Q4);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], Rollspeed);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Pitchspeed);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], Yawspeed);
    }

    /// <summary>Deserializes a <see cref="AttitudeQuaternion"/> from a raw payload span.</summary>
    public static AttitudeQuaternion Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Q1 = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            Q2 = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Q3 = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Q4 = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Rollspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            Pitchspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            Yawspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
        };
}
