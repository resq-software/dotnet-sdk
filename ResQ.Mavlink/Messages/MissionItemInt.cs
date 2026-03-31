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
/// MAVLink MISSION_ITEM_INT message (ID 73). Message encoding a mission item with sequence number.
/// Lat/lon represented as integer * 10^7.
/// </summary>
public readonly record struct MissionItemInt : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 37;

    /// <summary>PARAM1, see MAV_CMD enum.</summary>
    public float Param1 { get; init; }

    /// <summary>PARAM2, see MAV_CMD enum.</summary>
    public float Param2 { get; init; }

    /// <summary>PARAM3, see MAV_CMD enum.</summary>
    public float Param3 { get; init; }

    /// <summary>PARAM4, see MAV_CMD enum.</summary>
    public float Param4 { get; init; }

    /// <summary>PARAM5 / local: x position in meters * 1e4, global: latitude in degrees * 10^7.</summary>
    public int X { get; init; }

    /// <summary>PARAM6 / y position: local: x position in meters * 1e4, global: longitude in degrees *10^7.</summary>
    public int Y { get; init; }

    /// <summary>PARAM7 / z position: global: altitude in meters (relative or absolute, depending on frame).</summary>
    public float Z { get; init; }

    /// <summary>Waypoint ID (sequence number). Starts at zero.</summary>
    public ushort Seq { get; init; }

    /// <summary>The scheduled action for the waypoint.</summary>
    public MavCmd Command { get; init; }

    /// <summary>System ID.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>Component ID.</summary>
    public byte TargetComponent { get; init; }

    /// <summary>The coordinate system of the waypoint.</summary>
    public MavFrame Frame { get; init; }

    /// <summary>false:0, true:1.</summary>
    public byte Current { get; init; }

    /// <summary>Autocontinue to next wp.</summary>
    public byte Autocontinue { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 73;

    /// <inheritdoc/>
    public byte CrcExtra => 38;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteSingleLittleEndian(buffer, Param1);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Param2);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Param3);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Param4);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], X);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[20..], Y);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Z);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[28..], Seq);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[30..], (ushort)Command);
        buffer[32] = TargetSystem;
        buffer[33] = TargetComponent;
        buffer[34] = (byte)Frame;
        buffer[35] = Current;
        buffer[36] = Autocontinue;
    }

    /// <summary>Deserializes a <see cref="MissionItemInt"/> from a raw payload span.</summary>
    public static MissionItemInt Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new MissionItemInt
        {
            Param1 = BinaryPrimitives.ReadSingleLittleEndian(buffer),
            Param2 = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            Param3 = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Param4 = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            X = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]),
            Y = BinaryPrimitives.ReadInt32LittleEndian(buffer[20..]),
            Z = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            Seq = BinaryPrimitives.ReadUInt16LittleEndian(buffer[28..]),
            Command = (MavCmd)BinaryPrimitives.ReadUInt16LittleEndian(buffer[30..]),
            TargetSystem = buffer[32],
            TargetComponent = buffer[33],
            Frame = (MavFrame)buffer[34],
            Current = buffer[35],
            Autocontinue = buffer[36],
        };
    }
}
