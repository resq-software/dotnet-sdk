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
/// MAVLink VFR_HUD message (ID 74). Metrics typically displayed on a HUD for fixed-wing aircraft.
/// </summary>
public readonly record struct VfrHud : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 20;

    /// <summary>Current indicated airspeed (IAS).</summary>
    public float Airspeed { get; init; }

    /// <summary>Current ground speed.</summary>
    public float Groundspeed { get; init; }

    /// <summary>Current altitude (MSL).</summary>
    public float Alt { get; init; }

    /// <summary>Current climb rate.</summary>
    public float Climb { get; init; }

    /// <summary>Current heading in compass units (0-360, 0=north).</summary>
    public short Heading { get; init; }

    /// <summary>Current throttle setting (0 to 100).</summary>
    public ushort Throttle { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 74;

    /// <inheritdoc/>
    public byte CrcExtra => 20;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteSingleLittleEndian(buffer, Airspeed);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Groundspeed);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Alt);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Climb);
        BinaryPrimitives.WriteInt16LittleEndian(buffer[16..], Heading);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[18..], Throttle);
    }

    /// <summary>Deserializes a <see cref="VfrHud"/> from a raw payload span.</summary>
    public static VfrHud Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new VfrHud
        {
            Airspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer),
            Groundspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            Alt = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Climb = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Heading = BinaryPrimitives.ReadInt16LittleEndian(buffer[16..]),
            Throttle = BinaryPrimitives.ReadUInt16LittleEndian(buffer[18..]),
        };
    }
}
