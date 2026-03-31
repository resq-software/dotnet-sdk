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
/// MAVLink ATTITUDE_TARGET message (ID 83). Reports the current commanded attitude of the vehicle as specified by the autopilot.
/// </summary>
public readonly record struct AttitudeTarget : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 37;

    /// <summary>Timestamp (time since system boot) in ms.</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>Attitude quaternion (w, x, y, z order, zero-rotation is 1, 0, 0, 0).</summary>
    public float Q1 { get; init; }

    /// <summary>Quaternion component x.</summary>
    public float Q2 { get; init; }

    /// <summary>Quaternion component y.</summary>
    public float Q3 { get; init; }

    /// <summary>Quaternion component z.</summary>
    public float Q4 { get; init; }

    /// <summary>Body roll rate in rad/s.</summary>
    public float BodyRollRate { get; init; }

    /// <summary>Body pitch rate in rad/s.</summary>
    public float BodyPitchRate { get; init; }

    /// <summary>Body yaw rate in rad/s.</summary>
    public float BodyYawRate { get; init; }

    /// <summary>Collective thrust, normalized to 0..1.</summary>
    public float Thrust { get; init; }

    /// <summary>Bitmap to indicate which dimensions should be ignored by the vehicle.</summary>
    public byte TypeMask { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 83;

    /// <inheritdoc/>
    public byte CrcExtra => 22;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Q1);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Q2);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Q3);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Q4);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], BodyRollRate);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], BodyPitchRate);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], BodyYawRate);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[32..], Thrust);
        buffer[36] = TypeMask;
    }

    /// <summary>Deserializes a <see cref="AttitudeTarget"/> from a raw payload span.</summary>
    public static AttitudeTarget Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Q1 = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            Q2 = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Q3 = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Q4 = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            BodyRollRate = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            BodyPitchRate = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            BodyYawRate = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
            Thrust = BinaryPrimitives.ReadSingleLittleEndian(buffer[32..]),
            TypeMask = buffer[36],
        };
}
