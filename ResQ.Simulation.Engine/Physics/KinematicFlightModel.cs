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

    /// <summary>Maximum yaw slew rate in radians per second (~115°/s) — how fast heading turns.</summary>
    public const double MaxYawRateRadPerSec = 2.0;

    /// <summary>Horizontal speed (m/s) below which heading is held rather than chased from velocity.</summary>
    private const double HeadingHoldSpeed = 0.5;

    /// <summary>Maximum bank (roll) angle when turning, radians (~26°).</summary>
    private const double MaxBankRad = 0.45;

    /// <summary>Maximum nose pitch from forward speed, radians (~17°).</summary>
    private const double MaxPitchRad = 0.30;

    /// <summary>Rate (per second) at which roll/pitch ease toward their targets.</summary>
    private const double AttitudeEaseRate = 6.0;

    private readonly double _maxSpeed;
    private FlightCommand _currentCommand;
    private DronePhysicsState _state;

    // Attitude state, integrated each Step. Heading is the yaw about +Y (0 = +Z);
    // roll/pitch are eased cosmetic tilts derived from turn rate and forward speed.
    private double _headingRad;
    private double _rollRad;
    private double _pitchRad;

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
        // Any command other than Land re-arms a landed drone so it can take off
        // again — otherwise HasLanded latches on forever and Step() skips it,
        // leaving the drone frozen and unresponsive to further commands.
        if (command.Type != FlightCommandType.Land)
            HasLanded = false;

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
        var orientation = IntegrateAttitude(velocity, dt);

        _state = _state with
        {
            Position = position,
            Velocity = velocity,
            BatteryPercent = battery,
            Orientation = orientation,
        };

        // Landing check: only when actively landing and altitude is at or below threshold
        if (_currentCommand.Type == FlightCommandType.Land && position.Y <= LandedThreshold)
            HasLanded = true;
    }

    /// <summary>
    /// Advances the drone's attitude one step and returns the resulting body orientation.
    /// Heading follows the explicit <see cref="FlightCommand.DesiredYaw"/> when set, otherwise
    /// the direction of horizontal travel (held while nearly stationary); it slews at
    /// <see cref="MaxYawRateRadPerSec"/>. Roll banks into turns and pitch tips with forward
    /// speed — both eased and clamped for a natural, non-jerky look.
    /// </summary>
    private Quaternion IntegrateAttitude(Vector3 velocity, double dt)
    {
        var speed = new Vector2(velocity.X, velocity.Z).Length();

        double targetHeading = _currentCommand.DesiredYaw
            ?? (speed > HeadingHoldSpeed ? Math.Atan2(velocity.X, velocity.Z) : _headingRad);

        // Slew heading along the shortest arc, capped at the yaw rate.
        double delta = WrapPi(targetHeading - _headingRad);
        double maxStep = MaxYawRateRadPerSec * dt;
        double applied = Math.Clamp(delta, -maxStep, maxStep);
        _headingRad = WrapPi(_headingRad + applied);

        // Bank into the turn (roll ∝ turn rate); pitch nose-down with forward speed.
        double turnRate = dt > 0 ? applied / dt : 0.0;
        double targetRoll = Math.Clamp(-turnRate * 0.25, -MaxBankRad, MaxBankRad);
        double targetPitch = -Math.Clamp(speed / _maxSpeed, 0.0, 1.0) * MaxPitchRad;
        double ease = Math.Clamp(AttitudeEaseRate * dt, 0.0, 1.0);
        _rollRad += (targetRoll - _rollRad) * ease;
        _pitchRad += (targetPitch - _pitchRad) * ease;

        return Quaternion.CreateFromYawPitchRoll((float)_headingRad, (float)_pitchRad, (float)_rollRad);
    }

    /// <summary>Wraps an angle into (-π, π].</summary>
    private static double WrapPi(double angle)
    {
        angle %= 2 * Math.PI;
        if (angle > Math.PI) angle -= 2 * Math.PI;
        else if (angle <= -Math.PI) angle += 2 * Math.PI;
        return angle;
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
