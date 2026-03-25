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
/// A lightweight kinematic flight model that moves a simulated drone toward waypoints
/// at constant speed, applies wind disturbances, and drains battery over time.
/// </summary>
/// <remarks>
/// This model does not simulate aerodynamic forces or rotor dynamics.  It is suitable
/// for high-level mission planning tests, path coverage checks, and scenario validation
/// where physical accuracy is less important than deterministic, reproducible behaviour.
/// </remarks>
public sealed class KinematicFlightModel : IFlightModel
{
    /// <summary>Default maximum horizontal and vertical cruise speed in metres per second.</summary>
    public const double DefaultMaxSpeed = 15.0;

    /// <summary>Descent speed during landing in metres per second.</summary>
    public const double LandingSpeed = 2.0;

    /// <summary>Battery drain per simulated second as a percentage point.</summary>
    public const double BatteryDrainPerSec = 0.1;

    /// <summary>Altitude below which the drone is considered to have landed, in metres.</summary>
    public const double LandedThreshold = 0.5;

    /// <summary>Distance to a waypoint below which the drone considers the waypoint reached, in metres.</summary>
    public const double WaypointThreshold = 1.0;

    private readonly double _maxSpeed;
    private FlightCommand _currentCommand;
    private DronePhysicsState _state;

    /// <summary>
    /// Initialises a new <see cref="KinematicFlightModel"/> at the given start position.
    /// </summary>
    /// <param name="startPosition">The world-space launch position of the drone.</param>
    /// <param name="maxSpeed">
    /// The maximum cruise speed in metres per second.
    /// Defaults to <see cref="DefaultMaxSpeed"/> (15 m/s).
    /// </param>
    public KinematicFlightModel(Vector3 startPosition, double maxSpeed = DefaultMaxSpeed)
    {
        _maxSpeed = maxSpeed;
        LaunchPosition = startPosition;
        _state = DronePhysicsState.AtPosition(startPosition);
        _currentCommand = FlightCommand.Hover();
    }

    /// <inheritdoc />
    public DronePhysicsState State => _state;

    /// <inheritdoc />
    public Vector3 LaunchPosition { get; }

    /// <inheritdoc />
    public bool HasLanded { get; private set; }

    /// <inheritdoc />
    /// <remarks>
    /// A <see cref="FlightCommandType.ReturnToLaunch"/> command is immediately rewritten to a
    /// <see cref="FlightCommandType.GoToWaypoint"/> command targeting <see cref="LaunchPosition"/>,
    /// so <see cref="Step"/> never needs a separate RTL branch in its velocity computation.
    /// </remarks>
    public void ApplyCommand(FlightCommand command)
    {
        _currentCommand = command.Type == FlightCommandType.ReturnToLaunch
            ? FlightCommand.GoTo(LaunchPosition)
            : command;
    }

    /// <inheritdoc />
    public void Step(double dt, Vector3 wind)
    {
        if (HasLanded)
            return;

        var velocity = ComputeVelocity();
        var position = _state.Position + velocity * (float)dt + wind * (float)dt;

        // Clamp altitude to ground level
        if (position.Y < 0f)
            position = position with { Y = 0f };

        var battery = Math.Max(0.0, _state.BatteryPercent - BatteryDrainPerSec * dt);

        _state = _state with
        {
            Position = position,
            Velocity = velocity,
            BatteryPercent = battery,
        };

        // Landing check: only when actively landing and altitude is at or below threshold
        if (_currentCommand.Type == FlightCommandType.Land && position.Y <= LandedThreshold)
            HasLanded = true;
    }

    /// <summary>
    /// Computes the desired velocity vector for the current command and drone position.
    /// </summary>
    /// <returns>
    /// The target velocity in metres per second.  Zero for <see cref="FlightCommandType.Hover"/>;
    /// a downward vector at <see cref="LandingSpeed"/> for <see cref="FlightCommandType.Land"/>;
    /// or a unit vector toward the waypoint scaled by the effective speed for
    /// <see cref="FlightCommandType.GoToWaypoint"/>.
    /// </returns>
    private Vector3 ComputeVelocity()
    {
        return _currentCommand.Type switch
        {
            FlightCommandType.Hover => Vector3.Zero,
            FlightCommandType.Land => new Vector3(0f, -(float)LandingSpeed, 0f),
            FlightCommandType.GoToWaypoint => ComputeWaypointVelocity(),

            // RTL is rewritten in ApplyCommand; this branch should never be reached.
            _ => Vector3.Zero,
        };
    }

    /// <summary>
    /// Computes the velocity required to navigate toward the active waypoint.
    /// Returns <see cref="Vector3.Zero"/> if the drone is already within
    /// <see cref="WaypointThreshold"/> metres of the target.
    /// </summary>
    private Vector3 ComputeWaypointVelocity()
    {
        if (_currentCommand.TargetPosition is not { } target)
            return Vector3.Zero;

        var toTarget = target - _state.Position;
        if (toTarget.Length() <= WaypointThreshold)
            return Vector3.Zero;

        var speed = (float)(_currentCommand.DesiredSpeed ?? _maxSpeed);
        return Vector3.Normalize(toTarget) * speed;
    }
}
