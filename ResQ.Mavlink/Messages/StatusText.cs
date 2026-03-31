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

using System.Text;
using ResQ.Mavlink.Enums;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink STATUSTEXT message (ID 253). Status text message. These messages are printed in yellow in the COMM console of QGroundControl.
/// </summary>
public readonly record struct StatusText : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 51;

    /// <summary>Severity of status. Relies on the definitions within RFC-5424.</summary>
    public MavSeverity Severity { get; init; }

    /// <summary>Status text message (up to 50 characters, null-padded).</summary>
    public string Text { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 253;

    /// <inheritdoc/>
    public byte CrcExtra => 83;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        buffer[0] = (byte)Severity;
        // text: char[50], null-padded
        var textSpan = buffer[1..51];
        textSpan.Clear();
        if (!string.IsNullOrEmpty(Text))
        {
            Encoding.ASCII.GetBytes(Text.AsSpan(), textSpan);
        }
    }

    /// <summary>Deserializes a <see cref="StatusText"/> from a raw payload span.</summary>
    public static StatusText Deserialize(ReadOnlySpan<byte> buffer)
    {
        var textSpan = buffer[1..51];
        var nullIdx = textSpan.IndexOf((byte)0);
        var text = nullIdx >= 0
            ? Encoding.ASCII.GetString(textSpan[..nullIdx])
            : Encoding.ASCII.GetString(textSpan);

        return new StatusText
        {
            Severity = (MavSeverity)buffer[0],
            Text = text,
        };
    }
}
