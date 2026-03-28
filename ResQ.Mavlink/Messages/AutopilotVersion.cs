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
/// MAVLink AUTOPILOT_VERSION message (ID 148). Version and capability of autopilot software.
/// </summary>
public readonly record struct AutopilotVersion : IMavlinkMessage
{
    /// <summary>Payload size in bytes.</summary>
    public const int PayloadSize = 60;

    /// <summary>Bitmap of capabilities.</summary>
    public ulong Capabilities { get; init; }

    /// <summary>Firmware version number.</summary>
    public uint FlightSwVersion { get; init; }

    /// <summary>Middleware version number.</summary>
    public uint MiddlewareSwVersion { get; init; }

    /// <summary>Operating system version number.</summary>
    public uint OsSwVersion { get; init; }

    /// <summary>HW / board version (last 8 bytes should be silicon ID, if any).</summary>
    public uint BoardVersion { get; init; }

    /// <summary>UID if provided by hardware (see uid2).</summary>
    public ulong Uid { get; init; }

    /// <summary>ID of the board vendor.</summary>
    public ushort VendorId { get; init; }

    /// <summary>ID of the product.</summary>
    public ushort ProductId { get; init; }

    /// <inheritdoc/>
    public uint MessageId => 148;

    /// <inheritdoc/>
    public byte CrcExtra => 178;

    /// <inheritdoc/>
    public void Serialize(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, Capabilities);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], FlightSwVersion);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[12..], MiddlewareSwVersion);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[16..], OsSwVersion);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[20..], BoardVersion);
        // flight_custom_version[8] at bytes 24-31 - leave zero
        // middleware_custom_version[8] at bytes 32-39 - leave zero
        // os_custom_version[8] at bytes 40-47 - leave zero
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[48..], VendorId);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[50..], ProductId);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[52..], Uid);
    }

    /// <summary>Deserializes a <see cref="AutopilotVersion"/> from a raw payload span.</summary>
    public static AutopilotVersion Deserialize(ReadOnlySpan<byte> buffer) =>
        new()
        {
            Capabilities = BinaryPrimitives.ReadUInt64LittleEndian(buffer),
            FlightSwVersion = BinaryPrimitives.ReadUInt32LittleEndian(buffer[8..]),
            MiddlewareSwVersion = BinaryPrimitives.ReadUInt32LittleEndian(buffer[12..]),
            OsSwVersion = BinaryPrimitives.ReadUInt32LittleEndian(buffer[16..]),
            BoardVersion = BinaryPrimitives.ReadUInt32LittleEndian(buffer[20..]),
            VendorId = BinaryPrimitives.ReadUInt16LittleEndian(buffer[48..]),
            ProductId = BinaryPrimitives.ReadUInt16LittleEndian(buffer[50..]),
            Uid = BinaryPrimitives.ReadUInt64LittleEndian(buffer[52..]),
        };
}
