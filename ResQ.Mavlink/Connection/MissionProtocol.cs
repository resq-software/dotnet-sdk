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

using System.Collections.Generic;
using ResQ.Mavlink.Enums;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;
using ResQ.Mavlink.Transport;

namespace ResQ.Mavlink.Connection;

/// <summary>
/// Implements the MAVLink mission upload and download protocols.
/// </summary>
public sealed class MissionProtocol
{
    private readonly IMavlinkTransport _transport;
    private readonly byte _systemId;
    private readonly byte _componentId;
    private readonly TimeSpan _itemTimeout;
    private readonly int _maxRetries;
    private byte _sequence;

    /// <summary>
    /// Initializes a new <see cref="MissionProtocol"/>.
    /// </summary>
    /// <param name="transport">The MAVLink transport to communicate over.</param>
    /// <param name="systemId">The local system ID.</param>
    /// <param name="componentId">The local component ID.</param>
    /// <param name="itemTimeout">Timeout per item exchange. Default is 3 seconds.</param>
    /// <param name="maxRetries">Maximum retry attempts per item. Default is 3.</param>
    public MissionProtocol(
        IMavlinkTransport transport,
        byte systemId = 255,
        byte componentId = 190,
        TimeSpan? itemTimeout = null,
        int maxRetries = 3)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _systemId = systemId;
        _componentId = componentId;
        _itemTimeout = itemTimeout ?? TimeSpan.FromSeconds(3);
        _maxRetries = maxRetries;
    }

    /// <summary>
    /// Uploads a mission to a target system using the MAVLink mission upload protocol.
    /// </summary>
    /// <param name="items">The mission items to upload.</param>
    /// <param name="targetSystem">Target system ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="TimeoutException">Thrown if a response is not received within the configured timeout.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the mission upload fails.</exception>
    public async Task UploadMissionAsync(
        IReadOnlyList<MissionItemInt> items,
        byte targetSystem,
        CancellationToken ct = default)
    {
        // Step 1: Send MISSION_COUNT
        var countMsg = new MissionCount
        {
            Count = (ushort)items.Count,
            TargetSystem = targetSystem,
            TargetComponent = 1,
        };
        await SendMessageAsync(countMsg, ct).ConfigureAwait(false);

        // Step 2: Wait for MISSION_REQUEST for each item, send the item
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await foreach (var packet in _transport.ReceiveAsync(cts.Token).ConfigureAwait(false))
        {
            if (packet.MessageId == 40) // MISSION_REQUEST
            {
                var req = MissionRequest.Deserialize(packet.Payload.Span);
                if (req.Seq >= items.Count)
                    throw new InvalidOperationException($"Vehicle requested seq {req.Seq} but mission has only {items.Count} items.");

                await SendMessageWithRetryAsync(items[req.Seq], ct).ConfigureAwait(false);
            }
            else if (packet.MessageId == 51) // MISSION_REQUEST_INT
            {
                var req = MissionRequestInt.Deserialize(packet.Payload.Span);
                if (req.Seq >= items.Count)
                    throw new InvalidOperationException($"Vehicle requested seq {req.Seq} but mission has only {items.Count} items.");

                await SendMessageWithRetryAsync(items[req.Seq], ct).ConfigureAwait(false);
            }
            else if (packet.MessageId == 47) // MISSION_ACK
            {
                var ack = MissionAck.Deserialize(packet.Payload.Span);
                if (ack.Type != MavMissionResult.Accepted)
                    throw new InvalidOperationException($"Mission upload failed with result {ack.Type}.");

                await cts.CancelAsync().ConfigureAwait(false);
                return;
            }
        }
    }

    /// <summary>
    /// Downloads a mission from a target system using the MAVLink mission download protocol.
    /// </summary>
    /// <param name="targetSystem">Target system ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of downloaded mission items.</returns>
    /// <exception cref="TimeoutException">Thrown if a response is not received within the configured timeout.</exception>
    public async Task<List<MissionItemInt>> DownloadMissionAsync(byte targetSystem, CancellationToken ct = default)
    {
        // Step 1: Send MISSION_REQUEST_LIST
        var reqList = new MissionRequestList
        {
            TargetSystem = targetSystem,
            TargetComponent = 1,
        };
        await SendMessageWithRetryAsync(reqList, ct).ConfigureAwait(false);

        var items = new List<MissionItemInt>();
        int totalCount = -1;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await foreach (var packet in _transport.ReceiveAsync(cts.Token).ConfigureAwait(false))
        {
            if (packet.MessageId == 44 && totalCount < 0) // MISSION_COUNT
            {
                var count = MissionCount.Deserialize(packet.Payload.Span);
                totalCount = count.Count;

                if (totalCount == 0)
                {
                    await SendMissionAckAsync(targetSystem, MavMissionResult.Accepted, ct).ConfigureAwait(false);
                    await cts.CancelAsync().ConfigureAwait(false);
                    return items;
                }

                // Request first item
                await SendMissionRequestAsync(0, targetSystem, ct).ConfigureAwait(false);
            }
            else if (packet.MessageId == 73 && totalCount >= 0) // MISSION_ITEM_INT
            {
                var item = MissionItemInt.Deserialize(packet.Payload.Span);
                items.Add(item);

                if (items.Count < totalCount)
                {
                    await SendMissionRequestAsync((ushort)items.Count, targetSystem, ct).ConfigureAwait(false);
                }
                else
                {
                    // All items received
                    await SendMissionAckAsync(targetSystem, MavMissionResult.Accepted, ct).ConfigureAwait(false);
                    await cts.CancelAsync().ConfigureAwait(false);
                    return items;
                }
            }
        }

        return items;
    }

    private async Task SendMessageAsync(IMavlinkMessage message, CancellationToken ct)
    {
        var scratch = new byte[MavlinkConstants.MaxPayloadLength];
        message.Serialize(scratch);

        // Use the message's PayloadSize constant to avoid corrupting packets where 0x00 is valid data
        var payloadSize = GetMessagePayloadSize(message.GetType());
        var payload = scratch.AsMemory(0, payloadSize);

        var packet = new MavlinkPacket(
            sequenceNumber: _sequence++,
            systemId: _systemId,
            componentId: _componentId,
            messageId: message.MessageId,
            payload: payload,
            incompatFlags: 0,
            compatFlags: 0,
            signature: null);

        await _transport.SendAsync(packet, ct).ConfigureAwait(false);
    }

    private static int GetMessagePayloadSize(Type messageType)
    {
        var field = messageType.GetField("PayloadSize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase);
        return field != null ? (int)field.GetValue(null)! : MavlinkConstants.MaxPayloadLength;
    }

    private async Task SendMessageWithRetryAsync(IMavlinkMessage message, CancellationToken ct)
    {
        int retryCount = 0;
        while (retryCount <= _maxRetries)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(_itemTimeout);
                await SendMessageAsync(message, cts.Token).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (retryCount < _maxRetries)
            {
                retryCount++;
            }
        }
        throw new TimeoutException($"Failed to send message after {_maxRetries} retries within {_itemTimeout.TotalSeconds}s timeout.");
    }

    private Task SendMissionRequestAsync(ushort seq, byte targetSystem, CancellationToken ct) =>
        SendMessageAsync(new MissionRequest { Seq = seq, TargetSystem = targetSystem, TargetComponent = 1 }, ct);

    private Task SendMissionAckAsync(byte targetSystem, MavMissionResult result, CancellationToken ct) =>
        SendMessageAsync(new MissionAck { Type = result, TargetSystem = targetSystem, TargetComponent = 1 }, ct);
}
