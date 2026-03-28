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
/// MAVLink PARAM_REQUEST_READ message (ID 20). Request to read the onboard parameter with the param_id string id.
/// </summary>
public readonly record struct ParamRequestRead : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 20;

    /// <summary>Parameter index. Send -1 to use the param ID field as identifier (else the param id will be ignored).</summary>
    public short ParamIndex { get; init; }

    /// <summary>System ID.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>Component ID.</summary>
    public byte TargetComponent { get; init; }

    /// <summary>Onboard parameter id, terminated by NULL if the length is less than 16 human-readable chars and WITHOUT null termination (NULL) byte if the length is exactly 16 chars.</summary>
    public string ParamId { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 20;

    /// <inheritdoc/>
    public byte CrcExtra => 214;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt16LittleEndian(buffer, ParamIndex);
        buffer[2] = TargetSystem;
        buffer[3] = TargetComponent;
        // param_id: char[16]
        var idBytes = buffer[4..20];
        idBytes.Clear();
        if (!string.IsNullOrEmpty(ParamId))
        {
            Encoding.ASCII.GetBytes(ParamId.AsSpan(), idBytes);
        }
    }

    /// <summary>Deserializes a <see cref="ParamRequestRead"/> from a raw payload span.</summary>
    public static ParamRequestRead Deserialize(ReadOnlySpan<byte> buffer)
    {
        var idSpan = buffer[4..20];
        var nullIdx = idSpan.IndexOf((byte)0);
        var id = nullIdx >= 0
            ? Encoding.ASCII.GetString(idSpan[..nullIdx])
            : Encoding.ASCII.GetString(idSpan);

        return new ParamRequestRead
        {
            ParamIndex = BinaryPrimitives.ReadInt16LittleEndian(buffer),
            TargetSystem = buffer[2],
            TargetComponent = buffer[3],
            ParamId = id,
        };
    }
}
