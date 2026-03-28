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
/// MAVLink GPS_RTCM_DATA message (ID 233). RTCM message for injecting into the onboard GPS (targeted at the companion computer).
/// </summary>
public readonly record struct GpsRtcmData : IMavlinkMessage
{
    /// <summary>Payload size in bytes (2 + 180 = 182).</summary>
    public const int PayloadSize = 182;

    /// <summary>LSB: 1 means message is fragmented, next 2 bits are the fragment ID, the remaining 5 bits are used for the sequence ID.</summary>
    public byte Flags { get; init; }

    /// <summary>data length in bytes.</summary>
    public byte Len { get; init; }

    /// <summary>RTCM data (up to 180 bytes).</summary>
    public byte[] Data { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 233;

    /// <inheritdoc/>
    public byte CrcExtra => 35;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        buffer[0] = Flags;
        buffer[1] = Len;
        if (Data != null)
        {
            Data.AsSpan(0, Math.Min(Data.Length, 180)).CopyTo(buffer[2..]);
        }
    }

    /// <summary>Deserializes a <see cref="GpsRtcmData"/> from a raw payload span.</summary>
    public static GpsRtcmData Deserialize(ReadOnlySpan<byte> buffer)
    {
        var data = new byte[180];
        buffer[2..].CopyTo(data.AsSpan());
        return new()
        {
            Flags = buffer[0],
            Len = buffer[1],
            Data = data,
        };
    }
}
