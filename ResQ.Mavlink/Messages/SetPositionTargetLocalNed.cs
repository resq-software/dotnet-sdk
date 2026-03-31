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
using ResQ.Mavlink.Enums;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink SET_POSITION_TARGET_LOCAL_NED message (ID 84). Sets a desired vehicle position in a local north-east-down coordinate frame.
/// </summary>
public readonly record struct SetPositionTargetLocalNed : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 53;

    /// <summary>Timestamp (time since system boot) in ms.</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>X Position in NED frame in m.</summary>
    public float X { get; init; }

    /// <summary>Y Position in NED frame in m.</summary>
    public float Y { get; init; }

    /// <summary>Z Position in NED frame in m (positive is down).</summary>
    public float Z { get; init; }

    /// <summary>X velocity in NED frame in m/s.</summary>
    public float Vx { get; init; }

    /// <summary>Y velocity in NED frame in m/s.</summary>
    public float Vy { get; init; }

    /// <summary>Z velocity in NED frame in m/s.</summary>
    public float Vz { get; init; }

    /// <summary>X acceleration in NED frame in m/s^2.</summary>
    public float Afx { get; init; }

    /// <summary>Y acceleration in NED frame in m/s^2.</summary>
    public float Afy { get; init; }

    /// <summary>Z acceleration in NED frame in m/s^2.</summary>
    public float Afz { get; init; }

    /// <summary>yaw setpoint in rad.</summary>
    public float Yaw { get; init; }

    /// <summary>yaw rate setpoint in rad/s.</summary>
    public float YawRate { get; init; }

    /// <summary>Bitmap to indicate which dimensions should be ignored by the vehicle.</summary>
    public ushort TypeMask { get; init; }

    /// <summary>System ID.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>Component ID.</summary>
    public byte TargetComponent { get; init; }

    /// <summary>Valid options are: MAV_FRAME_LOCAL_NED = 1, MAV_FRAME_LOCAL_OFFSET_NED = 7, MAV_FRAME_BODY_NED = 8, MAV_FRAME_BODY_OFFSET_NED = 9.</summary>
    public MavFrame CoordinateFrame { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 84;

    /// <inheritdoc/>
    public byte CrcExtra => 143;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], X);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Y);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Z);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Vx);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], Vy);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Vz);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], Afx);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[32..], Afy);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[36..], Afz);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[40..], Yaw);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[44..], YawRate);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[48..], TypeMask);
        buffer[50] = TargetSystem;
        buffer[51] = TargetComponent;
        buffer[52] = (byte)CoordinateFrame;
    }

    /// <summary>Deserializes a <see cref="SetPositionTargetLocalNed"/> from a raw payload span.</summary>
    public static SetPositionTargetLocalNed Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            X = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            Y = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Z = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Vx = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Vy = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            Vz = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            Afx = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
            Afy = BinaryPrimitives.ReadSingleLittleEndian(buffer[32..]),
            Afz = BinaryPrimitives.ReadSingleLittleEndian(buffer[36..]),
            Yaw = BinaryPrimitives.ReadSingleLittleEndian(buffer[40..]),
            YawRate = BinaryPrimitives.ReadSingleLittleEndian(buffer[44..]),
            TypeMask = BinaryPrimitives.ReadUInt16LittleEndian(buffer[48..]),
            TargetSystem = buffer[50],
            TargetComponent = buffer[51],
            CoordinateFrame = (MavFrame)buffer[52],
        };
}
