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
/// MAVLink ESTIMATOR_STATUS message (ID 230). Estimator status message including EKF variances.
/// </summary>
public readonly record struct EstimatorStatus : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 42;

    /// <summary>Timestamp in microseconds.</summary>
    public ulong TimeUsec { get; init; }

    /// <summary>Velocity variance in m/s.</summary>
    public float VelRatio { get; init; }

    /// <summary>Horizontal position variance in m.</summary>
    public float PosHorizRatio { get; init; }

    /// <summary>Vertical position variance in m.</summary>
    public float PosVertRatio { get; init; }

    /// <summary>Magnetometer variance.</summary>
    public float MagRatio { get; init; }

    /// <summary>Height above terrain variance in m.</summary>
    public float HaglRatio { get; init; }

    /// <summary>True airspeed variance.</summary>
    public float TasRatio { get; init; }

    /// <summary>Horizontal position 1-STD accuracy in m.</summary>
    public float PosHorizAccuracy { get; init; }

    /// <summary>Vertical position 1-STD accuracy in m.</summary>
    public float PosVertAccuracy { get; init; }

    /// <summary>Integer bitmask indicating which EKF outputs are valid.</summary>
    public ushort Flags { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 230;

    /// <inheritdoc/>
    public byte CrcExtra => 163;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, TimeUsec);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], VelRatio);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], PosHorizRatio);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], PosVertRatio);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], MagRatio);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], HaglRatio);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[28..], TasRatio);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[32..], PosHorizAccuracy);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[36..], PosVertAccuracy);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[40..], Flags);
    }

    /// <summary>Deserializes a <see cref="EstimatorStatus"/> from a raw payload span.</summary>
    public static EstimatorStatus Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            TimeUsec = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            VelRatio = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            PosHorizRatio = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            PosVertRatio = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            MagRatio = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            HaglRatio = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
            TasRatio = BinaryPrimitives.ReadSingleLittleEndian(buffer[28..]),
            PosHorizAccuracy = BinaryPrimitives.ReadSingleLittleEndian(buffer[32..]),
            PosVertAccuracy = BinaryPrimitives.ReadSingleLittleEndian(buffer[36..]),
            Flags = BinaryPrimitives.ReadUInt16LittleEndian(buffer[40..]),
        };
}
