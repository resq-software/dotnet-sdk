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

using Microsoft.Extensions.Hosting;
using ResQ.Core;
using ResQ.Mavlink.Connection;
using ResQ.Mavlink.Gateway.State;
using ResQ.Mavlink.Messages;

namespace ResQ.Mavlink.Gateway;

/// <summary>
/// Represents the top-level MAVLink gateway orchestrator that manages vehicle connections,
/// routes messages between vehicles and the ResQ backend, and provides a telemetry feed.
/// </summary>
public interface IMavlinkGateway : IHostedService, IAsyncDisposable
{
    /// <summary>Gets a read-only dictionary of all connected vehicle systems keyed by MAVLink system ID.</summary>
    IReadOnlyDictionary<byte, MavlinkConnection> ConnectedSystems { get; }

    /// <summary>
    /// Sends a MAVLink message to the vehicle with the specified system ID.
    /// </summary>
    /// <param name="systemId">The target vehicle's MAVLink system ID.</param>
    /// <param name="msg">The message to send.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask SendToVehicleAsync(byte systemId, IMavlinkMessage msg, CancellationToken ct = default);

    /// <summary>
    /// Broadcasts a MAVLink message to all connected vehicles.
    /// </summary>
    /// <param name="msg">The message to broadcast.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask BroadcastAsync(IMavlinkMessage msg, CancellationToken ct = default);

    /// <summary>
    /// Returns an async enumerable stream of <see cref="TelemetryPacket"/> instances
    /// produced as vehicle telemetry is received and processed.
    /// </summary>
    /// <param name="ct">Cancellation token that ends the stream when cancelled.</param>
    /// <returns>An async sequence of telemetry packets.</returns>
    IAsyncEnumerable<TelemetryPacket> TelemetryFeed(CancellationToken ct = default);

    /// <summary>Gets the shared vehicle state tracker used by this gateway instance.</summary>
    VehicleStateTracker StateTracker { get; }
}
