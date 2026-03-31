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
/// MAVLink NAV_CONTROLLER_OUTPUT message (ID 62). The state of the fixed wing navigation and position controller.
/// </summary>
public readonly record struct NavControllerOutput : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 26;

    /// <summary>Current desired roll in degrees.</summary>
    public float NavRoll { get; init; }

    /// <summary>Current desired pitch in degrees.</summary>
    public float NavPitch { get; init; }

    /// <summary>Current altitude error in meters.</summary>
    public float AltError { get; init; }

    /// <summary>Current airspeed error in m/s.</summary>
    public float AspdError { get; init; }

    /// <summary>Current crosstrack error on x-y plane in meters.</summary>
    public float XtrackError { get; init; }

    /// <summary>Current desired heading in degrees.</summary>
    public short NavBearing { get; init; }

    /// <summary>Bearing to current waypoint/target in degrees.</summary>
    public short TargetBearing { get; init; }

    /// <summary>Distance to active waypoint in meters.</summary>
    public ushort WpDist { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 62;

    /// <inheritdoc/>
    public byte CrcExtra => 183;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteSingleLittleEndian(buffer, NavRoll);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], NavPitch);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], AltError);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], AspdError);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], XtrackError);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[20..], NavBearing);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[22..], TargetBearing);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[24..], WpDist);
    }

    /// <summary>Deserializes a <see cref="NavControllerOutput"/> from a raw payload span.</summary>
    public static NavControllerOutput Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            NavRoll = BinaryPrimitives.ReadSingleLittleEndian(buffer),
            NavPitch = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            AltError = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            AspdError = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            XtrackError = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            NavBearing = BinaryPrimitives.ReadInt16LittleEndian(buffer[20..]),
            TargetBearing = BinaryPrimitives.ReadInt16LittleEndian(buffer[22..]),
            WpDist = BinaryPrimitives.ReadUInt16LittleEndian(buffer[24..]),
        };
}
