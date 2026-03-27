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
using ResQ.Simulation.Engine.Physics;

namespace ResQ.Mavlink.Sitl;

/// <summary>
/// Adapts an <see cref="IFlightModel"/> to the <see cref="IFlightBackend"/> interface,
/// allowing local physics models to be used wherever a flight backend is required.
/// </summary>
/// <remarks>
/// This adapter is useful for unit-testing and offline simulation where no SITL process
/// is needed. The wrapped flight model is not owned by the adapter and will not be
/// disposed when <see cref="DisposeAsync"/> is called.
/// </remarks>
public sealed class FlightModelBackendAdapter : IFlightBackend
{
    private readonly IFlightModel _model;

    /// <summary>
    /// Initialises a new <see cref="FlightModelBackendAdapter"/> wrapping <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The flight model to delegate physics to.</param>
    public FlightModelBackendAdapter(IFlightModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="FlightModelBackendAdapter"/> exposes no optional capabilities — it is a
    /// bare-minimum adapter without GPS or wind-injection support beyond what the underlying
    /// flight model already provides.
    /// </remarks>
    public FlightBackendCapabilities Capabilities => FlightBackendCapabilities.None;

    /// <inheritdoc/>
    /// <remarks>
    /// This adapter wraps an already-constructed flight model, so initialisation is a no-op.
    /// The <paramref name="config"/> parameter is accepted for interface compatibility.
    /// </remarks>
    public ValueTask InitializeAsync(DroneConfig config, CancellationToken ct = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc/>
    public ValueTask<DronePhysicsState> StepAsync(double dt, Vector3 wind, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _model.Step(dt, wind);
        return ValueTask.FromResult(_model.State);
    }

    /// <inheritdoc/>
    public ValueTask SendCommandAsync(FlightCommand command, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _model.ApplyCommand(command);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The underlying <see cref="IFlightModel"/> is not owned by this adapter and is not disposed.
    /// </remarks>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
