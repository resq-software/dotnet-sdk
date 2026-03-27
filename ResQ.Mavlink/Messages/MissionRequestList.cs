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

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink MISSION_REQUEST_LIST message (ID 43). Request the overall list of mission items from the system/component.
/// </summary>
public readonly record struct MissionRequestList : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 2;

    /// <summary>System ID.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>Component ID.</summary>
    public byte TargetComponent { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 43;

    /// <inheritdoc/>
    public byte CrcExtra => 132;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        buffer[0] = TargetSystem;
        buffer[1] = TargetComponent;
    }

    /// <summary>Deserializes a <see cref="MissionRequestList"/> from a raw payload span.</summary>
    public static MissionRequestList Deserialize(ReadOnlySpan<byte> buffer) =>
        new() { TargetSystem = buffer[0], TargetComponent = buffer[1] };
}
