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
            [0]   = buf => Heartbeat.Deserialize(buf),
            [1]   = buf => SysStatus.Deserialize(buf),
            [2]   = buf => SystemTime.Deserialize(buf),
            [4]   = buf => Ping.Deserialize(buf),
            [11]  = buf => SetMode.Deserialize(buf),
            [20]  = buf => ParamRequestRead.Deserialize(buf),
            [22]  = buf => ParamValue.Deserialize(buf),
            [23]  = buf => ParamSet.Deserialize(buf),
            [24]  = buf => GpsRawInt.Deserialize(buf),
            [26]  = buf => ScaledImu.Deserialize(buf),
            [27]  = buf => RawImu.Deserialize(buf),
            [29]  = buf => ScaledPressure.Deserialize(buf),
            [30]  = buf => Attitude.Deserialize(buf),
            [32]  = buf => LocalPositionNed.Deserialize(buf),
            [33]  = buf => GlobalPositionInt.Deserialize(buf),
            [36]  = buf => ServoOutputRaw.Deserialize(buf),
            [40]  = buf => MissionRequest.Deserialize(buf),
            [41]  = buf => MissionSetCurrent.Deserialize(buf),
            [42]  = buf => MissionCurrent.Deserialize(buf),
            [43]  = buf => MissionRequestList.Deserialize(buf),
            [44]  = buf => MissionCount.Deserialize(buf),
            [45]  = buf => MissionClearAll.Deserialize(buf),
            [47]  = buf => MissionAck.Deserialize(buf),
            [49]  = buf => GpsGlobalOrigin.Deserialize(buf),
            [51]  = buf => MissionRequestInt.Deserialize(buf),
            [61]  = buf => AttitudeQuaternion.Deserialize(buf),
            [62]  = buf => NavControllerOutput.Deserialize(buf),
            [63]  = buf => GlobalPositionIntCov.Deserialize(buf),
            [70]  = buf => RcChannelsOverride.Deserialize(buf),
            [73]  = buf => MissionItemInt.Deserialize(buf),
            [74]  = buf => VfrHud.Deserialize(buf),
            [76]  = buf => CommandLong.Deserialize(buf),
            [77]  = buf => CommandAck.Deserialize(buf),
            [83]  = buf => AttitudeTarget.Deserialize(buf),
            [84]  = buf => SetPositionTargetLocalNed.Deserialize(buf),
            [85]  = buf => PositionTargetLocalNed.Deserialize(buf),
            [86]  = buf => SetPositionTargetGlobalInt.Deserialize(buf),
            [87]  = buf => PositionTargetGlobalInt.Deserialize(buf),
            [105] = buf => HighresImu.Deserialize(buf),
            [109] = buf => RadioStatus.Deserialize(buf),
            [111] = buf => Timesync.Deserialize(buf),
            [116] = buf => ScaledImu2.Deserialize(buf),
            [125] = buf => PowerStatus.Deserialize(buf),
            [133] = buf => TerrainRequest.Deserialize(buf),
            [134] = buf => TerrainData.Deserialize(buf),
            [135] = buf => TerrainCheck.Deserialize(buf),
            [136] = buf => TerrainReport.Deserialize(buf),
            [140] = buf => ActuatorControlTarget.Deserialize(buf),
            [147] = buf => BatteryStatus.Deserialize(buf),
            [148] = buf => AutopilotVersion.Deserialize(buf),
            [230] = buf => EstimatorStatus.Deserialize(buf),
            [231] = buf => WindCov.Deserialize(buf),
            [233] = buf => GpsRtcmData.Deserialize(buf),
            [241] = buf => Vibration.Deserialize(buf),
            [242] = buf => HomePosition.Deserialize(buf),
            [245] = buf => ExtendedSysState.Deserialize(buf),
            [253] = buf => StatusText.Deserialize(buf),
            [263] = buf => CameraImageCaptured.Deserialize(buf),
            [265] = buf => MountOrientation.Deserialize(buf),
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
