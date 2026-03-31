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
/// MAVLink POSITION_TARGET_GLOBAL_INT message (ID 87).
/// Reports the current commanded vehicle position, velocity, and acceleration as specified by the autopilot.
/// </summary>
public readonly record struct PositionTargetGlobalInt : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 51;

    /// <summary>Timestamp (time since system boot).</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>X Position in WGS84 frame in 1e7 * meters.</summary>
    public int LatInt { get; init; }

    /// <summary>Y Position in WGS84 frame in 1e7 * meters.</summary>
    public int LonInt { get; init; }

    /// <summary>Altitude in meters in AMSL altitude (not WGS84 if absolute or relative).</summary>
    public float Alt { get; init; }

    /// <summary>X velocity in NED frame.</summary>
    public float Vx { get; init; }

    /// <summary>Y velocity in NED frame.</summary>
    public float Vy { get; init; }

    /// <summary>Z velocity in NED frame.</summary>
    public float Vz { get; init; }

    /// <summary>X acceleration or force (if bit 10 of type_mask is set) in NED frame.</summary>
    public float Afx { get; init; }

    /// <summary>Y acceleration or force (if bit 10 of type_mask is set) in NED frame.</summary>
    public float Afy { get; init; }

    /// <summary>Z acceleration or force (if bit 10 of type_mask is set) in NED frame.</summary>
    public float Afz { get; init; }

    /// <summary>yaw setpoint.</summary>
    public float Yaw { get; init; }

    /// <summary>yaw rate setpoint.</summary>
    public float YawRate { get; init; }

    /// <summary>Bitmap to indicate which dimensions should be ignored by the vehicle.</summary>
    public ushort TypeMask { get; init; }

    /// <summary>Valid options are: MAV_FRAME_GLOBAL_INT = 5, MAV_FRAME_GLOBAL_RELATIVE_ALT_INT = 6, MAV_FRAME_GLOBAL_TERRAIN_ALT_INT = 11.</summary>
    public MavFrame CoordinateFrame { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 87;

    /// <inheritdoc/>
    public byte CrcExtra => 150;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], LatInt);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], LonInt);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Alt);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Vx);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], Vy);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Vz);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], Afx);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[32..], Afy);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[36..], Afz);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[40..], Yaw);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[44..], YawRate);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[48..], TypeMask);
        buffer[50] = (byte)CoordinateFrame;
    }

    /// <summary>Deserializes a <see cref="PositionTargetGlobalInt"/> from a raw payload span.</summary>
    public static PositionTargetGlobalInt Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new PositionTargetGlobalInt
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            LatInt = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
            LonInt = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
            Alt = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Vx = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Vy = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            Vz = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            Afx = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
            Afy = BinaryPrimitives.ReadSingleLittleEndian(buffer[32..]),
            Afz = BinaryPrimitives.ReadSingleLittleEndian(buffer[36..]),
            Yaw = BinaryPrimitives.ReadSingleLittleEndian(buffer[40..]),
            YawRate = BinaryPrimitives.ReadSingleLittleEndian(buffer[44..]),
            TypeMask = BinaryPrimitives.ReadUInt16LittleEndian(buffer[48..]),
            CoordinateFrame = (MavFrame)buffer[50],
        };
    }
}
