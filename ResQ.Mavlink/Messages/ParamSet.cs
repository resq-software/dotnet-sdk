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
/// MAVLink PARAM_SET message (ID 23). Set a parameter value (write new value to permanent storage).
/// </summary>
public readonly record struct ParamSet : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 23;

    /// <summary>Onboard parameter value.</summary>
    public float ParamValue { get; init; }

    /// <summary>System ID.</summary>
    public byte TargetSystem { get; init; }

    /// <summary>Component ID.</summary>
    public byte TargetComponent { get; init; }

    /// <summary>Onboard parameter id (up to 16 chars).</summary>
    public string ParamId { get; init; }

    /// <summary>Onboard parameter type.</summary>
    public byte ParamType { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 23;

    /// <inheritdoc/>
    public byte CrcExtra => 168;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteSingleLittleEndian(buffer, ParamValue);
        buffer[4] = TargetSystem;
        buffer[5] = TargetComponent;
        // param_id: char[16] — truncate to 16 chars to prevent buffer overrun.
        var idBytes = buffer[6..22];
        idBytes.Clear();
        if (!string.IsNullOrEmpty(ParamId))
        {
            var id = ParamId.Length > 16 ? ParamId[..16] : ParamId;
            Encoding.ASCII.GetBytes(id.AsSpan(), idBytes);
        }
        buffer[22] = ParamType;
    }

    /// <summary>Deserializes a <see cref="ParamSet"/> from a raw payload span.</summary>
    public static ParamSet Deserialize(ReadOnlySpan<byte> buffer)
    {
        var idSpan = buffer[6..22];
        var nullIdx = idSpan.IndexOf((byte)0);
        var id = nullIdx >= 0
            ? Encoding.ASCII.GetString(idSpan[..nullIdx])
            : Encoding.ASCII.GetString(idSpan);

        return new ParamSet
        {
            ParamValue = BinaryPrimitives.ReadSingleLittleEndian(buffer),
            TargetSystem = buffer[4],
            TargetComponent = buffer[5],
            ParamId = id,
            ParamType = buffer[22],
        };
    }
}
