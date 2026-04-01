using System.Numerics;
using Microsoft.Extensions.Options;
using ResQ.Mavlink.Dialect;
using ResQ.Mavlink.Dialect.Messages;
using ResQ.Mavlink.Dialect.Enums;
using ResQ.Mavlink.Mesh;
using ResQ.Mavlink.Mesh.Simulation;
using ResQ.Simulation.Engine.Core;
using ResQ.Simulation.Engine.Environment;
using ResQ.Simulation.Engine.Physics;

// Register the custom ResQ MAVLink dialect
ResqDialectRegistry.Register();

Console.WriteLine("=== ResQ Simulation Demo ===\n");

// ── 1. Physics Simulation ──────────────────────────────────────────

Console.WriteLine("--- Drone Swarm Simulation (Quadrotor Physics) ---\n");

var config = new SimulationConfig
{
    FlightModel = FlightModelType.Quadrotor,
    DeltaTime = 1.0 / 60.0,
    Seed = 42,
};

var weather = new WeatherSystem(new WeatherConfig(
    Mode: WeatherMode.Steady,
    WindSpeed: 3.0,
    WindDirection: 45.0
));

var world = new SimulationWorld(Options.Create(config), new FlatTerrain(), weather);

// Spawn 5 drones in a line
for (var i = 0; i < 5; i++)
{
    var drone = world.AddDrone($"drone-{i}", new Vector3(i * 100, 0, 0));
    drone.SendCommand(FlightCommand.GoTo(new Vector3(i * 100, 50, 200))); // fly to 50m alt, 200m ahead
}

Console.WriteLine($"Spawned {world.Drones.Count} drones\n");

// Run 10 seconds of simulation
for (var tick = 0; tick < 600; tick++)
{
    world.Step();

    if (tick % 60 == 0) // log every second
    {
        var t = world.Clock.ElapsedTime;
        Console.WriteLine($"  t={t:F1}s");
        foreach (var drone in world.Drones)
        {
            var s = drone.FlightModel.State;
            Console.WriteLine($"    [{drone.Id}] pos=({s.Position.X:F0}, {s.Position.Y:F0}, {s.Position.Z:F0}) " +
                              $"vel={s.Velocity.Length():F1}m/s bat={s.BatteryPercent:F0}%");
        }
    }
}

// ── 2. Radio Model ─────────────────────────────────────────────────

Console.WriteLine("\n--- Mesh Radio Simulation ---\n");

var radio = new RadioModel(Options.Create(new RadioModelOptions
{
    MaxRangeMetres = 500,
    AttenuationFactor = 2.0f,
}));

var positions = new[]
{
    ("Alpha", new Vector3(0, 50, 0)),
    ("Bravo", new Vector3(300, 50, 0)),
    ("Charlie", new Vector3(650, 50, 0)),
    ("Delta", new Vector3(1200, 50, 0)),
};

Console.WriteLine("  Drone positions and connectivity (range=500m):\n");
for (var i = 0; i < positions.Length; i++)
{
    var (name, pos) = positions[i];
    Console.Write($"  {name} ({pos.X}m)");

    var links = new List<string>();
    for (var j = 0; j < positions.Length; j++)
    {
        if (i == j) continue;
        var dist = Vector3.Distance(pos, positions[j].Item2);
        var canTalk = radio.CanCommunicate(pos, positions[j].Item2);
        if (canTalk)
        {
            var signal = radio.GetSignalStrength(pos, positions[j].Item2);
            links.Add($"{positions[j].Item1} ({dist:F0}m, signal={signal:F2})");
        }
    }

    Console.WriteLine(links.Count > 0
        ? $" --> {string.Join(", ", links)}"
        : " --> [isolated]");
}

// ── 3. Mesh Neighbor Table ─────────────────────────────────────────

Console.WriteLine("\n--- Mesh Neighbor Table ---\n");

var neighborTable = new MeshNeighborTable(Options.Create(new MeshNeighborTableOptions()));

neighborTable.Update(1, -45, true);  // drone 1: strong signal, has ground link
neighborTable.Update(2, -60, false); // drone 2: moderate signal, no ground link
neighborTable.Update(3, -80, false); // drone 3: weak signal, no ground link

var neighbors = neighborTable.GetNeighbors();
Console.WriteLine($"  Active neighbors: {neighbors.Count}");
foreach (var n in neighbors)
    Console.WriteLine($"    System {n.SystemId}: RSSI={n.Rssi}dBm, ground={n.HasGroundLink}");
Console.WriteLine($"  Partitioned: {neighborTable.IsPartitioned}");

var topoMsg = neighborTable.BuildTopologyMessage(255);
Console.WriteLine($"  Topology message: {topoMsg.NeighborCount} neighbors reported");

// ── 4. Custom Dialect Messages ─────────────────────────────────────

Console.WriteLine("\n--- ResQ Dialect Messages ---\n");

var detection = new ResqDetection
{
    DetectionType = ResqDetectionType.Fire,
    Confidence = 92,
    LatE7 = 473977418,
    LonE7 = 85255792,
    AltMm = 408000,
    TimestampMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
};

var beacon = new ResqEmergencyBeacon
{
    BeaconId = 1,
    LatE7 = 473977418,
    LonE7 = 85255792,
    AltMm = 50000,
    BeaconType = ResqBeaconType.PersonInDistress,
    Urgency = ResqUrgencyLevel.LifeThreatening,
    TimestampMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    Ttl = 7,
};

Console.WriteLine($"  Detection: {detection.DetectionType} at ({detection.LatE7 / 1e7:F6}, {detection.LonE7 / 1e7:F6}), confidence={detection.Confidence}%");
Console.WriteLine($"  Emergency: {beacon.BeaconType}, urgency={beacon.Urgency}, TTL={beacon.Ttl} hops");

// Round-trip test
var buf = new byte[ResqDetection.PayloadSize];
detection.Serialize(buf);
var parsed = ResqDetection.Deserialize(buf);
Console.WriteLine($"  Round-trip: type={parsed.DetectionType}, confidence={parsed.Confidence}% [OK]");

Console.WriteLine("\n=== Demo Complete ===");
