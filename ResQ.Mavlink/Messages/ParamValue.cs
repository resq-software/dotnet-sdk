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
using System.Text;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// MAVLink PARAM_VALUE message (ID 22). Emit the value of a onboard parameter.
/// </summary>
public readonly record struct ParamValue : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 25;

    /// <summary>Onboard parameter value.</summary>
    public float ParamValue_ { get; init; }

    /// <summary>Total number of onboard parameters.</summary>
    public ushort ParamCount { get; init; }

    /// <summary>Index of this onboard parameter.</summary>
    public ushort ParamIndex { get; init; }

    /// <summary>Onboard parameter id (up to 16 chars, null-terminated if shorter).</summary>
    public string ParamId { get; init; }

    /// <summary>Onboard parameter type.</summary>
    public byte ParamType { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 22;

    /// <inheritdoc/>
    public byte CrcExtra => 220;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteSingleLittleEndian(buffer, ParamValue_);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..], ParamCount);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[6..], ParamIndex);
        // param_id: char[16]
        var idBytes = buffer[8..24];
        idBytes.Clear();
        if (!string.IsNullOrEmpty(ParamId))
        {
            Encoding.ASCII.GetBytes(ParamId.AsSpan(), idBytes);
        }
        buffer[24] = ParamType;
    }

    /// <summary>Deserializes a <see cref="ParamValue"/> from a raw payload span.</summary>
    public static ParamValue Deserialize(ReadOnlySpan<byte> buffer)
    {
        var idSpan = buffer[8..24];
        var nullIdx = idSpan.IndexOf((byte)0);
        var id = nullIdx >= 0
            ? Encoding.ASCII.GetString(idSpan[..nullIdx])
            : Encoding.ASCII.GetString(idSpan);

        return new ParamValue
        {
            ParamValue_ = BinaryPrimitives.ReadSingleLittleEndian(buffer),
            ParamCount = BinaryPrimitives.ReadUInt16LittleEndian(buffer[4..]),
            ParamIndex = BinaryPrimitives.ReadUInt16LittleEndian(buffer[6..]),
            ParamId = id,
            ParamType = buffer[24],
        };
    }
}
