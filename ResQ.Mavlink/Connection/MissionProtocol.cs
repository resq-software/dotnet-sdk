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

        // Step 2: Wait for MISSION_REQUEST for each item, send the item (with per-item timeout and retry).
        IMavlinkMessage? lastRequest = null;
        int retries = 0;

        while (true)
        {
            using var itemCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            itemCts.CancelAfter(_itemTimeout);

            MavlinkPacket? received = null;
            try
            {
                await foreach (var packet in _transport.ReceiveAsync(itemCts.Token).ConfigureAwait(false))
                {
                    if (packet.MessageId is 40 or 51 or 47)
                    {
                        received = packet;
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Item-level timeout — retry if possible.
                if (++retries > _maxRetries)
                    throw new TimeoutException($"No response from vehicle after {_maxRetries} retries during mission upload.");

                if (lastRequest is not null)
                    await SendMessageAsync(lastRequest, ct).ConfigureAwait(false);
                continue;
            }

            if (received is null)
                break;

            retries = 0;

            if (received.MessageId == 40) // MISSION_REQUEST
            {
                var req = MissionRequest.Deserialize(received.Payload.Span);
                if (req.Seq >= items.Count)
                    throw new InvalidOperationException($"Vehicle requested seq {req.Seq} but mission has only {items.Count} items.");

                lastRequest = items[req.Seq];
                await SendMessageAsync(lastRequest, ct).ConfigureAwait(false);
            }
            else if (received.MessageId == 51) // MISSION_REQUEST_INT
            {
                var req = MissionRequestInt.Deserialize(received.Payload.Span);
                if (req.Seq >= items.Count)
                    throw new InvalidOperationException($"Vehicle requested seq {req.Seq} but mission has only {items.Count} items.");

                lastRequest = items[req.Seq];
                await SendMessageAsync(lastRequest, ct).ConfigureAwait(false);
            }
            else if (received.MessageId == 47) // MISSION_ACK
            {
                var ack = MissionAck.Deserialize(received.Payload.Span);
                if (ack.Type != MavMissionResult.Accepted)
                    throw new InvalidOperationException($"Mission upload failed with result {ack.Type}.");

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
        await SendMessageAsync(reqList, ct).ConfigureAwait(false);

        var items = new List<MissionItemInt>();
        int totalCount = -1;
        int retries = 0;
        IMavlinkMessage? lastRequest = reqList;

        while (true)
        {
            using var itemCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            itemCts.CancelAfter(_itemTimeout);

            MavlinkPacket? received = null;
            try
            {
                await foreach (var packet in _transport.ReceiveAsync(itemCts.Token).ConfigureAwait(false))
                {
                    if (packet.MessageId is 44 or 73)
                    {
                        received = packet;
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                if (++retries > _maxRetries)
                    throw new TimeoutException($"No response from vehicle after {_maxRetries} retries during mission download.");

                if (lastRequest is not null)
                    await SendMessageAsync(lastRequest, ct).ConfigureAwait(false);
                continue;
            }

            if (received is null)
                break;

            retries = 0;

            if (received.MessageId == 44 && totalCount < 0) // MISSION_COUNT
            {
                var count = MissionCount.Deserialize(received.Payload.Span);
                totalCount = count.Count;

                if (totalCount == 0)
                {
                    await SendMissionAckAsync(targetSystem, MavMissionResult.Accepted, ct).ConfigureAwait(false);
                    return items;
                }

                // Request first item
                lastRequest = new MissionRequest { Seq = 0, TargetSystem = targetSystem, TargetComponent = 1 };
                await SendMessageAsync(lastRequest, ct).ConfigureAwait(false);
            }
            else if (received.MessageId == 73 && totalCount >= 0) // MISSION_ITEM_INT
            {
                var item = MissionItemInt.Deserialize(received.Payload.Span);
                items.Add(item);

                if (items.Count < totalCount)
                {
                    lastRequest = new MissionRequest { Seq = (ushort)items.Count, TargetSystem = targetSystem, TargetComponent = 1 };
                    await SendMessageAsync(lastRequest, ct).ConfigureAwait(false);
                }
                else
                {
                    // All items received
                    await SendMissionAckAsync(targetSystem, MavMissionResult.Accepted, ct).ConfigureAwait(false);
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
        // Use the full serialized buffer — do NOT trim trailing zeros.
        // Fields such as 0.0f serialize as 00 00 00 00 and trimming corrupts the packet.
        var payload = scratch.AsMemory(0, scratch.Length);

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

    private Task SendMissionRequestAsync(ushort seq, byte targetSystem, CancellationToken ct) =>
        SendMessageAsync(new MissionRequest { Seq = seq, TargetSystem = targetSystem, TargetComponent = 1 }, ct);

    private Task SendMissionAckAsync(byte targetSystem, MavMissionResult result, CancellationToken ct) =>
        SendMessageAsync(new MissionAck { Type = result, TargetSystem = targetSystem, TargetComponent = 1 }, ct);
}
