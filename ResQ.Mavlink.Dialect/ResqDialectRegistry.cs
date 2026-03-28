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

using ResQ.Mavlink.Dialect.Messages;
using ResQ.Mavlink.Messages;
using ResQ.Mavlink.Protocol;

namespace ResQ.Mavlink.Dialect;

/// <summary>
/// One-time registration of all ResQ custom dialect messages (IDs 60000–60007).
/// Call <see cref="Register"/> once at application startup before processing any MAVLink packets.
/// </summary>
/// <remarks>
/// Registration is thread-safe and idempotent — subsequent calls overwrite with identical values.
/// CRC extra values are hardcoded constants derived from each message's field layout.
/// </remarks>
public static class ResqDialectRegistry
{
    // CRC extra constants — one per message, computed from field name+type hashes per MAVLink spec.
    // These are fixed at dialect version 1; any field layout change requires a new dialect version.
    private const byte CrcExtraDetection = 142; // RESQ_DETECTION
    private const byte CrcExtraDetectionAck = 73;  // RESQ_DETECTION_ACK
    private const byte CrcExtraSwarmTask = 211; // RESQ_SWARM_TASK
    private const byte CrcExtraSwarmTaskAck = 55;  // RESQ_SWARM_TASK_ACK
    private const byte CrcExtraHazardZone = 188; // RESQ_HAZARD_ZONE
    private const byte CrcExtraMeshTopology = 97;  // RESQ_MESH_TOPOLOGY
    private const byte CrcExtraDroneCap = 44;  // RESQ_DRONE_CAPABILITY
    private const byte CrcExtraEmergBeacon = 161; // RESQ_EMERGENCY_BEACON

    /// <summary>
    /// Registers all ResQ dialect CRC extras and deserializers into the global MAVLink tables.
    /// </summary>
    public static void Register()
    {
        // Register CRC extras so the codec can validate incoming dialect packets.
        MavlinkCrc.RegisterCrcExtra(60000, CrcExtraDetection);
        MavlinkCrc.RegisterCrcExtra(60001, CrcExtraDetectionAck);
        MavlinkCrc.RegisterCrcExtra(60002, CrcExtraSwarmTask);
        MavlinkCrc.RegisterCrcExtra(60003, CrcExtraSwarmTaskAck);
        MavlinkCrc.RegisterCrcExtra(60004, CrcExtraHazardZone);
        MavlinkCrc.RegisterCrcExtra(60005, CrcExtraMeshTopology);
        MavlinkCrc.RegisterCrcExtra(60006, CrcExtraDroneCap);
        MavlinkCrc.RegisterCrcExtra(60007, CrcExtraEmergBeacon);

        // Register deserializers so MessageRegistry can produce typed messages.
        MessageRegistry.Register(60000, buf => ResqDetection.Deserialize(buf));
        MessageRegistry.Register(60001, buf => ResqDetectionAck.Deserialize(buf));
        MessageRegistry.Register(60002, buf => ResqSwarmTask.Deserialize(buf));
        MessageRegistry.Register(60003, buf => ResqSwarmTaskAck.Deserialize(buf));
        MessageRegistry.Register(60004, buf => ResqHazardZone.Deserialize(buf));
        MessageRegistry.Register(60005, buf => ResqMeshTopology.Deserialize(buf));
        MessageRegistry.Register(60006, buf => ResqDroneCapability.Deserialize(buf));
        MessageRegistry.Register(60007, buf => ResqEmergencyBeacon.Deserialize(buf));
    }
}
