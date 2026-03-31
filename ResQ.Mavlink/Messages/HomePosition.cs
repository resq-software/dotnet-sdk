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
/// MAVLink HOME_POSITION message (ID 242). This message can be requested by sending the MAV_CMD_GET_HOME_POSITION command.
/// Fields: latitude, longitude, altitude (int32 degE7/mm), x/y/z (float local), q[4] (float quaternion),
/// approach_x/y/z (float approach vector).
/// </summary>
public readonly record struct HomePosition : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 52;

    /// <summary>Latitude (WGS84), in degrees * 1E7.</summary>
    public int Latitude { get; init; }

    /// <summary>Longitude (WGS84), in degrees * 1E7.</summary>
    public int Longitude { get; init; }

    /// <summary>Altitude (AMSL), in meters * 1000 (positive for up).</summary>
    public int Altitude { get; init; }

    /// <summary>Local X position of the home position in the EKF local coordinate system (meters).</summary>
    public float X { get; init; }

    /// <summary>Local Y position of the home position in the EKF local coordinate system (meters).</summary>
    public float Y { get; init; }

    /// <summary>Local Z position of the home position in the EKF local coordinate system (meters).</summary>
    public float Z { get; init; }

    /// <summary>World to surface normal and target to body orientation (Quaternion) — Q1.</summary>
    public float Q1 { get; init; }

    /// <summary>World to surface normal and target to body orientation (Quaternion) — Q2.</summary>
    public float Q2 { get; init; }

    /// <summary>World to surface normal and target to body orientation (Quaternion) — Q3.</summary>
    public float Q3 { get; init; }

    /// <summary>World to surface normal and target to body orientation (Quaternion) — Q4.</summary>
    public float Q4 { get; init; }

    /// <summary>Local X position of the end of the approach vector.</summary>
    public float ApproachX { get; init; }

    /// <summary>Local Y position of the end of the approach vector.</summary>
    public float ApproachY { get; init; }

    /// <summary>Local Z position of the end of the approach vector.</summary>
    public float ApproachZ { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 242;

    /// <inheritdoc/>
    public byte CrcExtra => 104;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, Latitude);       // 0
        BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], Longitude); // 4
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], Altitude);  // 8
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], X);       // 12
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Y);       // 16
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], Z);       // 20
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Q1);      // 24
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], Q2);      // 28
        BinaryPrimitives.WriteSingleLittleEndian(buffer[32..], Q3);      // 32
        BinaryPrimitives.WriteSingleLittleEndian(buffer[36..], Q4);      // 36
        BinaryPrimitives.WriteSingleLittleEndian(buffer[40..], ApproachX); // 40
        BinaryPrimitives.WriteSingleLittleEndian(buffer[44..], ApproachY); // 44
        BinaryPrimitives.WriteSingleLittleEndian(buffer[48..], ApproachZ); // 48
        // total: 52 bytes
    }

    /// <summary>Deserializes a <see cref="HomePosition"/> from a raw payload span.</summary>
    public static HomePosition Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new HomePosition
        {
            Latitude = BinaryPrimitives.ReadInt32LittleEndian(buffer),
            Longitude = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
            Altitude = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
            X = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Y = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Z = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            Q1 = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            Q2 = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
            Q3 = BinaryPrimitives.ReadSingleLittleEndian(buffer[32..]),
            Q4 = BinaryPrimitives.ReadSingleLittleEndian(buffer[36..]),
            ApproachX = BinaryPrimitives.ReadSingleLittleEndian(buffer[40..]),
            ApproachY = BinaryPrimitives.ReadSingleLittleEndian(buffer[44..]),
            ApproachZ = BinaryPrimitives.ReadSingleLittleEndian(buffer[48..]),
        };
    }
}
