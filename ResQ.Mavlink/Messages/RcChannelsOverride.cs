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
/// MAVLink RC_CHANNELS_OVERRIDE message (ID 70). Override RC channels for MAV link controlled GCS.
/// </summary>
public readonly record struct RcChannelsOverride : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 18;

    /// <summary>RC channel 1 value. A value of UINT16_MAX means to ignore this field.</summary>
    public ushort Chan1Raw { get; init; }

    /// <summary>RC channel 2 value. A value of UINT16_MAX means to ignore this field.</summary>
    public ushort Chan2Raw { get; init; }

    /// <summary>RC channel 3 value. A value of UINT16_MAX means to ignore this field.</summary>
    public ushort Chan3Raw { get; init; }

    /// <summary>RC channel 4 value. A value of UINT16_MAX means to ignore this field.</summary>
    public ushort Chan4Raw { get; init; }

    /// <summary>RC channel 5 value. A value of UINT16_MAX means to ignore this field.</summary>
    public ushort Chan5Raw { get; init; }

    /// <summary>RC channel 6 value. A value of UINT16_MAX means to ignore this field.</summary>
    public ushort Chan6Raw { get; init; }

    /// <summary>RC channel 7 value. A value of UINT16_MAX means to ignore this field.</summary>
    public ushort Chan7Raw { get; init; }

    /// <summary>RC channel 8 value. A value of UINT16_MAX means to ignore this field.</summary>
    public ushort Chan8Raw { get; init; }

    /// <summary>System ID.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>Component ID.</summary>
    public byte TargetComponent { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 70;

    /// <inheritdoc/>
    public byte CrcExtra => 124;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, Chan1Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], Chan2Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..], Chan3Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[6..], Chan4Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[8..], Chan5Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[10..], Chan6Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[12..], Chan7Raw);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[14..], Chan8Raw);
        buffer[16] = TargetSystem;
        buffer[17] = TargetComponent;
    }

    /// <summary>Deserializes a <see cref="RcChannelsOverride"/> from a raw payload span.</summary>
    public static RcChannelsOverride Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new RcChannelsOverride
        {
            Chan1Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer),
            Chan2Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[2..]),
            Chan3Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[4..]),
            Chan4Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[6..]),
            Chan5Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[8..]),
            Chan6Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[10..]),
            Chan7Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[12..]),
            Chan8Raw = BinaryPrimitives.ReadUInt16LittleEndian(buffer[14..]),
            TargetSystem = buffer[16],
            TargetComponent = buffer[17],
        };
    }
}
