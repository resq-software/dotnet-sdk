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
/// MAVLink EXTENDED_SYS_STATE message (ID 245). Provides state for additional features.
/// </summary>
public readonly record struct ExtendedSysState : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 2;

    /// <summary>The VTOL state if applicable. Is set to MAV_VTOL_STATE_UNDEFINED if UAV is not in VTOL configuration.</summary>
    public byte VtolState { get; init; }

    /// <summary>The landed state. Is set to MAV_LANDED_STATE_UNDEFINED if landed state is unknown.</summary>
    public byte LandedState { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 245;

    /// <inheritdoc/>
    public byte CrcExtra => 130;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        buffer[0] = VtolState;
        buffer[1] = LandedState;
    }

    /// <summary>Deserializes an <see cref="ExtendedSysState"/> from a raw payload span.</summary>
    public static ExtendedSysState Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new ExtendedSysState
        {
            VtolState = buffer[0],
            LandedState = buffer[1],
        };
    }
}
