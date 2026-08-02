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

using System.Numerics;

namespace ResQ.Simulation.Engine.Physics;

/// <summary>
/// Identifies the high-level behaviour requested of a simulated drone.
/// </summary>
public enum FlightCommandType
{
    /// <summary>Hold the current position; maintain altitude.</summary>
    Hover,

    /// <summary>Fly to an explicit 3-D waypoint.</summary>
    GoToWaypoint,

    /// <summary>Return to the recorded launch position and land.</summary>
    ReturnToLaunch,

    /// <summary>Descend and land at the current horizontal position.</summary>
    Land,
}

/// <summary>
/// An immutable command issued to a flight model, describing the desired drone behaviour
/// for the current control step.
/// </summary>
/// <param name="Type">The kind of flight manoeuvre to execute.</param>
/// <param name="TargetPosition">
/// The desired destination in world space.
/// Required when <see cref="Type"/> is <see cref="FlightCommandType.GoToWaypoint"/>;
/// ignored otherwise.
/// </param>
/// <param name="DesiredSpeed">
/// The cruise speed in metres per second for waypoint navigation.
/// When <see langword="null"/> the flight model uses its configured default maximum speed.
/// </param>
/// <param name="DesiredYaw">
/// The commanded heading in radians about the world +Y axis (0 = facing +Z), used to
/// point/rotate the drone independently of its travel direction. When <see langword="null"/>
/// the flight model faces the direction of travel (or holds heading while stationary).
/// </param>
public readonly record struct FlightCommand(
    FlightCommandType Type,
    Vector3? TargetPosition = null,
    double? DesiredSpeed = null,
    double? DesiredYaw = null)
{
    /// <summary>
    /// Creates a <see cref="FlightCommand"/> that tells the drone to hold its current position.
    /// </summary>
    /// <param name="yaw">Optional commanded heading in radians; <see langword="null"/> holds the current heading.</param>
    /// <returns>A hover command.</returns>
    public static FlightCommand Hover(double? yaw = null) => new(FlightCommandType.Hover, DesiredYaw: yaw);

    /// <summary>
    /// Creates a <see cref="FlightCommand"/> that tells the drone to fly to <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The world-space destination.</param>
    /// <param name="speed">
    /// Optional cruise speed in m/s. Defaults to the flight model's maximum speed when <see langword="null"/>.
    /// </param>
    /// <param name="yaw">Optional commanded heading in radians; <see langword="null"/> faces the direction of travel.</param>
    /// <returns>A go-to-waypoint command.</returns>
    public static FlightCommand GoTo(Vector3 target, double? speed = null, double? yaw = null) =>
        new(FlightCommandType.GoToWaypoint, target, speed, yaw);

    /// <summary>
    /// Creates a <see cref="FlightCommand"/> that instructs the drone to return to its launch position.
    /// </summary>
    /// <returns>A return-to-launch command.</returns>
    public static FlightCommand RTL() => new(FlightCommandType.ReturnToLaunch);

    /// <summary>
    /// Creates a <see cref="FlightCommand"/> that instructs the drone to land at its current position.
    /// </summary>
    /// <returns>A land command.</returns>
    public static FlightCommand Land() => new(FlightCommandType.Land);
}
