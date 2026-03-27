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
/// MAVLink ATTITUDE message (ID 30). Roll/pitch/yaw in radians, rates in rad/s.
/// </summary>
public readonly record struct Attitude : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 28;

    /// <summary>Timestamp (time since system boot).</summary>
    public uint TimeBootMs { get; init; }

    /// <summary>Roll angle (-pi..+pi).</summary>
    public float Roll { get; init; }

    /// <summary>Pitch angle (-pi..+pi).</summary>
    public float Pitch { get; init; }

    /// <summary>Yaw angle (-pi..+pi).</summary>
    public float Yaw { get; init; }

    /// <summary>Roll angular speed.</summary>
    public float Rollspeed { get; init; }

    /// <summary>Pitch angular speed.</summary>
    public float Pitchspeed { get; init; }

    /// <summary>Yaw angular speed.</summary>
    public float Yawspeed { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 30;

    /// <inheritdoc/>
    public byte CrcExtra => 39;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, TimeBootMs);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Roll);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Pitch);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Yaw);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[16..], Rollspeed);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[20..], Pitchspeed);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[24..], Yawspeed);
    }

    /// <summary>Deserializes an <see cref="Attitude"/> from a raw payload span.</summary>
    /// <param name="buffer">Raw payload bytes (must be at least <see cref="PayloadSize"/> bytes).</param>
    /// <returns>The deserialized <see cref="Attitude"/>.</returns>
    public static Attitude Deserialize(ReadOnlySpan<byte> buffer)
    {
        return new Attitude
        {
            TimeBootMs = BinaryPrimitives.ReadUInt32LittleEndian(buffer),
            Roll = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            Pitch = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            Yaw = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]),
            Rollspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[16..]),
            Pitchspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[20..]),
            Yawspeed = BinaryPrimitives.ReadSingleLittleEndian(buffer[24..]),
        };
    }
}
