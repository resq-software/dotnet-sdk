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
    /// <summary>Maximum RTCM data payload length in bytes.</summary>
    public const int MaxDataLength = 180;

    /// <summary>Payload size in bytes (2 + 180 = 182).</summary>
    public const int PayloadSize = 182;

    /// <summary>LSB: 1 means message is fragmented, next 2 bits are the fragment ID, the remaining 5 bits are used for the sequence ID.</summary>
    public byte Flags { get; init; }

    /// <summary>data length in bytes.</summary>
    public byte Len { get; init; }

    /// <summary>RTCM message data (up to 180 bytes).</summary>
    public byte[]? Data { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 233;

    /// <inheritdoc/>
    public byte CrcExtra => 35;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        buffer[0] = Flags;
        buffer[1] = Len;
        // 180 data bytes at offset 2
        var dataSlice = buffer.Slice(2, MaxDataLength);
        dataSlice.Clear();
        if (Data is not null)
        {
            var copyLen = Math.Min(Data.Length, MaxDataLength);
            Data.AsSpan(0, copyLen).CopyTo(dataSlice);
        }
    }

    /// <summary>Deserializes a <see cref="GpsRtcmData"/> from a raw payload span.</summary>
    public static GpsRtcmData Deserialize(ReadOnlySpan<byte> buffer)
    {
        var len = buffer[1];
        var dataLen = Math.Min((int)len, MaxDataLength);
        var data = new byte[dataLen];
        if (buffer.Length >= 2 + dataLen)
            buffer.Slice(2, dataLen).CopyTo(data);

        return new GpsRtcmData
        {
            Flags = buffer[0],
            Len = len,
            Data = data,
        };
    }
}
