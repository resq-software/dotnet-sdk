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
/// MAVLink WIND_COV message (ID 231). Wind covariance estimate from vehicle.
/// </summary>
public readonly record struct WindCov : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 40;

    /// <summary>Timestamp in microseconds.</summary>
    public ulong TimeUsec { get; init; }

    /// <summary>Wind in X (NED) direction in m/s.</summary>
    public float WindX { get; init; }

    /// <summary>Wind in Y (NED) direction in m/s.</summary>
    public float WindY { get; init; }

    /// <summary>Wind in Z (NED) direction in m/s.</summary>
    public float WindZ { get; init; }

    /// <summary>Variability of wind in XY in m/s.</summary>
    public float VarHoriz { get; init; }

    /// <summary>Variability of wind in Z in m/s.</summary>
    public float VarVert { get; init; }

    /// <summary>AMSL altitude (m) this measurement was taken at.</summary>
    public float WindAlt { get; init; }

    /// <summary>Horizontal speed 1-STD accuracy in m/s.</summary>
    public float HorizAccuracy { get; init; }

    /// <summary>Vertical speed 1-STD accuracy in m/s.</summary>
    public float VertAccuracy { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 231;

    /// <inheritdoc/>
    public byte CrcExtra => 105;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUsec);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], WindX);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], WindY);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], WindZ);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], VarHoriz);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], VarVert);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], WindAlt);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[32..], HorizAccuracy);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[36..], VertAccuracy);
    }

    /// <summary>Deserializes a <see cref="WindCov"/> from a raw payload span.</summary>
    public static WindCov Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUsec = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            WindX = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            WindY = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            WindZ = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            VarHoriz = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            VarVert = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            WindAlt = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
            HorizAccuracy = BinaryPrimitives.ReadSingleLittleEndian(buffer[32..]),
            VertAccuracy = BinaryPrimitives.ReadSingleLittleEndian(buffer[36..]),
        };
}
