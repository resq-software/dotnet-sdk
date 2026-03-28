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

using System;
using System.Numerics;

namespace ResQ.Simulation.Engine.Physics;

/// <summary>
/// A high-fidelity 6DOF quadrotor flight model with PD control and aerodynamic forces.
/// </summary>
/// <remarks>
/// This model simulates gravity, rotor thrust via a PD controller, aerodynamic drag,
/// and wind disturbances. It integrates equations of motion using the semi-implicit
/// Euler method, suitable for scenario validation where physical realism matters.
/// </remarks>
public sealed class QuadrotorFlightModel : IFlightModel
{
    /// <summary>Gravitational acceleration in metres per second squared.</summary>
    public const double Gravity = 9.81;

    /// <summary>Aerodynamic drag coefficient (dimensionless).</summary>
    public const double DragCoefficient = 0.5;

    /// <summary>Maximum thrust produced by a single rotor in Newtons.</summary>
    public const double MaxThrustPerRotor = 15.0;

    /// <summary>Number of rotors on the quadrotor.</summary>
    public const int NumRotors = 4;

    /// <summary>Controlled descent speed during landing in metres per second.</summary>
    public const double LandingDescentSpeed = 2.0;

    /// <summary>Altitude below which the drone is considered landed, in metres.</summary>
    public const double LandedAltitudeThreshold = 0.5;

    /// <summary>Distance to a waypoint below which it is considered reached, in metres.</summary>
    public const double WaypointReachedThreshold = 2.0;

    /// <summary>Base battery drain per second as a percentage point, independent of thrust.</summary>
    public const double BatteryDrainBase = 0.02;

    /// <summary>Additional battery drain per Newton of thrust per second.</summary>
    public const double BatteryDrainThrustFactor = 0.005;

    /// <summary>Proportional gain for the PD altitude/position controller.</summary>
    public const double PidGainP = 4.0;

    /// <summary>Derivative gain for the PD altitude/position controller.</summary>
    public const double PidGainD = 3.0;

    private readonly double _mass;
    private readonly double _maxTotalThrust;
    private FlightCommand _currentCommand;
    private DronePhysicsState _state;

    /// <summary>
    /// Initialises a new <see cref="QuadrotorFlightModel"/> at the given start position.
    /// </summary>
    /// <param name="startPosition">The world-space launch position of the drone.</param>
    /// <param name="mass">The mass of the drone in kilograms. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="mass"/> is less than or equal to zero.
    /// </exception>
    public QuadrotorFlightModel(Vector3 startPosition, double mass)
    {
        if (mass <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(mass), mass, "Mass must be greater than zero.");

        _mass = mass;
        _maxTotalThrust = MaxThrustPerRotor * NumRotors;
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
    /// <see cref="FlightCommandType.GoToWaypoint"/> command targeting <see cref="LaunchPosition"/>.
    /// </remarks>
    public void ApplyCommand(FlightCommand command)
    {
        _currentCommand = command.Type == FlightCommandType.ReturnToLaunch
            ? FlightCommand.GoTo(LaunchPosition)
            : command;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses semi-implicit Euler integration: velocity is updated before position.
    /// Forces applied each step: thrust (PD-controlled), gravity, aerodynamic drag,
    /// and a wind disturbance proportional to the drone's mass.
    /// </remarks>
    public void Step(double dt, Vector3 wind)
    {
        if (HasLanded)
            return;

        var vel = _state.Velocity;

        // 1. Compute rotor thrust force via PD controller
        var thrustForce = ComputeThrust(dt);
        var thrustMagnitude = thrustForce.Length();

        // 2. Gravity
        var gravityForce = new Vector3(0f, (float)(-_mass * Gravity), 0f);

        // 3. Aerodynamic drag: opposes velocity, proportional to v^2
        var speed = vel.Length();
        var dragForce = speed > 0.001f
            ? -Vector3.Normalize(vel) * (float)(DragCoefficient * speed * speed)
            : Vector3.Zero;

        // 4. Wind acts as a body force proportional to mass (wind acceleration * mass)
        var windForce = wind * (float)(_mass * 0.1);

        // 5. Net force and acceleration
        var netForce = thrustForce + gravityForce + dragForce + windForce;
        var acceleration = netForce / (float)_mass;

        // 6. Semi-implicit Euler: update velocity first, then position
        vel += acceleration * (float)dt;
        var pos = _state.Position + vel * (float)dt;

        // 7. Ground clamp: cannot go below Y = 0
        if (pos.Y < 0f)
        {
            pos = pos with { Y = 0f };
            if (vel.Y < 0f)
                vel = vel with { Y = 0f };
        }

        // 8. Battery drain based on base rate plus thrust contribution
        var battery = Math.Max(
            0.0,
            _state.BatteryPercent - (BatteryDrainBase + BatteryDrainThrustFactor * thrustMagnitude) * dt);

        _state = _state with
        {
            Position = pos,
            Velocity = vel,
            BatteryPercent = battery,
        };

        // 9. Landing detection: only when Land command is active and altitude is at/below threshold
        if (_currentCommand.Type == FlightCommandType.Land && pos.Y <= LandedAltitudeThreshold)
            HasLanded = true;
    }

    /// <summary>
    /// Computes the thrust force vector for the current command using a PD controller.
    /// </summary>
    /// <param name="dt">The current timestep duration in seconds (unused; reserved for future integral term).</param>
    /// <returns>The thrust force vector in Newtons (world space).</returns>
    private Vector3 ComputeThrust(double dt)
    {
        _ = dt; // reserved for future integral term

        var vel = _state.Velocity;
        var pos = _state.Position;
        var gravityComp = (float)(_mass * Gravity); // Newtons needed to counteract gravity

        return _currentCommand.Type switch
        {
            FlightCommandType.Hover => ComputeHoverThrust(vel, gravityComp),
            FlightCommandType.GoToWaypoint => ComputeWaypointThrust(pos, vel, gravityComp),
            FlightCommandType.Land => ComputeLandingThrust(vel, gravityComp),

            // RTL is rewritten in ApplyCommand; this branch should never be reached
            _ => new Vector3(0f, gravityComp, 0f),
        };
    }

    /// <summary>
    /// Computes thrust to hold the current altitude (hover).
    /// Uses gravity compensation plus a damping term on vertical velocity.
    /// </summary>
    private Vector3 ComputeHoverThrust(Vector3 vel, float gravityComp)
    {
        // Counteract gravity, plus damp vertical velocity
        var verticalThrust = gravityComp - (float)(PidGainD * vel.Y);
        var clampedVertical = Math.Clamp(verticalThrust, 0f, (float)_maxTotalThrust);
        return new Vector3(0f, (float)clampedVertical, 0f);
    }

    /// <summary>
    /// Computes thrust to navigate toward the active waypoint via a full 3D PD controller.
    /// Includes gravity compensation on the vertical axis.
    /// </summary>
    private Vector3 ComputeWaypointThrust(Vector3 pos, Vector3 vel, float gravityComp)
    {
        if (_currentCommand.TargetPosition is not { } target)
            return new Vector3(0f, gravityComp, 0f);

        var error = target - pos;
        if (error.Length() <= WaypointReachedThreshold)
        {
            // Reached: switch to hover-like hold
            return ComputeHoverThrust(vel, gravityComp);
        }

        // PD: P * error - D * velocity + gravity compensation on Y axis
        var thrustX = (float)(PidGainP * error.X - PidGainD * vel.X);
        var thrustY = (float)(PidGainP * error.Y - PidGainD * vel.Y) + gravityComp;
        var thrustZ = (float)(PidGainP * error.Z - PidGainD * vel.Z);

        // Clamp total thrust magnitude to physical limit
        var thrust = new Vector3(thrustX, thrustY, thrustZ);
        var magnitude = thrust.Length();
        if (magnitude > (float)_maxTotalThrust)
            thrust = thrust / magnitude * (float)_maxTotalThrust;

        return thrust;
    }

    /// <summary>
    /// Computes thrust for a controlled descent, targeting a downward velocity of
    /// <see cref="LandingDescentSpeed"/> m/s, with gravity compensation.
    /// </summary>
    private Vector3 ComputeLandingThrust(Vector3 vel, float gravityComp)
    {
        // Target descent rate: -LandingDescentSpeed (downward)
        var targetVy = -(float)LandingDescentSpeed;
        var velError = targetVy - vel.Y;
        var verticalThrust = gravityComp + (float)(PidGainP * velError);
        var clampedVertical = Math.Clamp(verticalThrust, 0f, (float)_maxTotalThrust);
        return new Vector3(0f, (float)clampedVertical, 0f);
    }
}
