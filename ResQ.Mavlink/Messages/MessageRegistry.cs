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

using System.Collections.Concurrent;

namespace ResQ.Mavlink.Messages;

/// <summary>
/// Registry mapping message IDs to deserialization functions.
/// </summary>
/// <remarks>
/// Uses <c>byte[]</c> in delegate signatures because <c>ReadOnlySpan&lt;byte&gt;</c> cannot
/// be captured as a generic type argument. Callers pass <c>payload.ToArray()</c> or the
/// backing array from <c>ReadOnlyMemory&lt;byte&gt;</c>.
/// </remarks>
public static class MessageRegistry
{
    private static readonly ConcurrentDictionary<uint, Func<byte[], IMavlinkMessage>> Deserializers = new(
        new Dictionary<uint, Func<byte[], IMavlinkMessage>>
        {
            [0] = buf => Heartbeat.Deserialize(buf),
            [1] = buf => SysStatus.Deserialize(buf),
            [11] = buf => SetMode.Deserialize(buf),
            [20] = buf => ParamRequestRead.Deserialize(buf),
            [22] = buf => ParamValue.Deserialize(buf),
            [23] = buf => ParamSet.Deserialize(buf),
            [24] = buf => GpsRawInt.Deserialize(buf),
            [30] = buf => Attitude.Deserialize(buf),
            [33] = buf => GlobalPositionInt.Deserialize(buf),
            [40] = buf => MissionRequest.Deserialize(buf),
            [42] = buf => MissionCurrent.Deserialize(buf),
            [44] = buf => MissionCount.Deserialize(buf),
            [47] = buf => MissionAck.Deserialize(buf),
            [51] = buf => MissionRequestInt.Deserialize(buf),
            [70] = buf => RcChannelsOverride.Deserialize(buf),
            [73] = buf => MissionItemInt.Deserialize(buf),
            [74] = buf => VfrHud.Deserialize(buf),
            [76] = buf => CommandLong.Deserialize(buf),
            [77] = buf => CommandAck.Deserialize(buf),
            [86] = buf => SetPositionTargetGlobalInt.Deserialize(buf),
            [87] = buf => PositionTargetGlobalInt.Deserialize(buf),
            [242] = buf => HomePosition.Deserialize(buf),
            [245] = buf => ExtendedSysState.Deserialize(buf),
            [253] = buf => StatusText.Deserialize(buf),
        });

    /// <summary>
    /// Attempts to deserialize a payload into a typed message.
    /// </summary>
    /// <param name="messageId">The MAVLink message ID.</param>
    /// <param name="payload">Raw payload bytes.</param>
    /// <param name="message">The deserialized message, or <c>null</c> if the message ID is not registered.</param>
    /// <returns><c>true</c> if deserialization succeeded; <c>false</c> if the message ID is unknown.</returns>
    public static bool TryDeserialize(uint messageId, byte[] payload, out IMavlinkMessage? message)
    {
        if (Deserializers.TryGetValue(messageId, out var factory))
        {
            message = factory(payload);
            return true;
        }
        message = null;
        return false;
    }

    /// <summary>
    /// Overload accepting <see cref="ReadOnlySpan{T}"/> — converts to array internally.
    /// </summary>
    /// <param name="messageId">The MAVLink message ID.</param>
    /// <param name="payload">Raw payload bytes.</param>
    /// <param name="message">The deserialized message, or <c>null</c> if the message ID is not registered.</param>
    /// <returns><c>true</c> if deserialization succeeded; <c>false</c> if the message ID is unknown.</returns>
    public static bool TryDeserialize(uint messageId, ReadOnlySpan<byte> payload, out IMavlinkMessage? message)
        => TryDeserialize(messageId, payload.ToArray(), out message);

    /// <summary>Returns whether a message ID has a registered deserializer.</summary>
    /// <param name="messageId">The MAVLink message ID.</param>
    /// <returns><c>true</c> if registered.</returns>
    public static bool IsRegistered(uint messageId) => Deserializers.ContainsKey(messageId);

    /// <summary>
    /// Registers a custom deserializer. Thread-safe. Used by dialect extensions.
    /// </summary>
    /// <param name="messageId">The MAVLink message ID to register.</param>
    /// <param name="deserializer">Factory function mapping a byte array to a typed message.</param>
    public static void Register(uint messageId, Func<byte[], IMavlinkMessage> deserializer) =>
        Deserializers[messageId] = deserializer;
}
