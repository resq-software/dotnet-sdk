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

using ResQ.Mavlink.Enums;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink MISSION_ACK message (ID 47). Ack message during waypoint handling.
/// </summary>
public readonly record struct MissionAck : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 3;

    /// <summary>System ID.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>Component ID.</summary>
    public byte TargetComponent { get; init; }

    /// <summary>Mission result.</summary>
    public MavMissionResult Type { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 47;

    /// <inheritdoc/>
    public byte CrcExtra => 153;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        buffer[0] = TargetSystem;
        buffer[1] = TargetComponent;
        buffer[2] = (byte)Type;
    }

    /// <summary>Deserializes a <see cref="MissionAck"/> from a raw payload span.</summary>
    public static MissionAck Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new MissionAck
        {
            TargetSystem = buffer[0],
            TargetComponent = buffer[1],
            Type = (MavMissionResult)buffer[2],
        };
    }
}
