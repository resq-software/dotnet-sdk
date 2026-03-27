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
/// MAVLink GLOBAL_POSITION_INT_COV message (ID 63). The filtered global position (e.g. fused GPS and accelerometers) with uncertainty.
/// </summary>
public readonly record struct GlobalPositionIntCov : IMavlinkMessage
{
    /// <summary>Payload size in bytes (simplified, without covariance matrix).</summary>
    public const int PayloadSize = 40;

    /// <summary>Timestamp in microseconds.</summary>
    public ulong TimeUsec { get; init; }

    /// <summary>Latitude in degrees * 1e7.</summary>
    public int Lat { get; init; }

    /// <summary>Longitude in degrees * 1e7.</summary>
    public int Lon { get; init; }

    /// <summary>Altitude in mm (MSL).</summary>
    public int Alt { get; init; }

    /// <summary>Altitude above ground in mm.</summary>
    public int RelativeAlt { get; init; }

    /// <summary>Ground X velocity in cm/s.</summary>
    public float Vx { get; init; }

    /// <summary>Ground Y velocity in cm/s.</summary>
    public float Vy { get; init; }

    /// <summary>Ground Z velocity in cm/s.</summary>
    public float Vz { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 63;

    /// <inheritdoc/>
    public byte CrcExtra => 119;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUsec);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], Lat);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], Lon);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], Alt);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[20..], RelativeAlt);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Vx);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], Vy);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[32..], Vz);
    }

    /// <summary>Deserializes a <see cref="GlobalPositionIntCov"/> from a raw payload span.</summary>
    public static GlobalPositionIntCov Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUsec = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            Lat = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
            Lon = BinaryPrimitives.ReadInt32LittleEndian(buffer[12..]),
            Alt = BinaryPrimitives.ReadInt32LittleEndian(buffer[16..]),
            RelativeAlt = BinaryPrimitives.ReadInt32LittleEndian(buffer[20..]),
            Vx = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            Vy = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
            Vz = BinaryPrimitives.ReadSingleLittleEndian(buffer[32..]),
        };
}
