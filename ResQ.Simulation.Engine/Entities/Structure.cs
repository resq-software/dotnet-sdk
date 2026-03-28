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

namespace ResQ.Simulation.Engine.Entities;

/// <summary>
/// Describes the physical condition of a <see cref="Structure"/> at a given point in the simulation.
/// </summary>
public enum DamageState
{
    /// <summary>The structure is undamaged and fully functional.</summary>
    Intact,

    /// <summary>The structure has sustained partial damage but has not collapsed.</summary>
    Damaged,

    /// <summary>The structure has fully collapsed.</summary>
    Collapsed,

    /// <summary>The structure is submerged or significantly flooded.</summary>
    Flooded,

    /// <summary>The structure is actively on fire.</summary>
    OnFire,
}

/// <summary>
/// Represents a static obstacle or building in the simulation world, defined by its
/// axis-aligned bounding box and current <see cref="DamageState"/>.
/// </summary>
public sealed class Structure
{
    /// <summary>Gets the unique identifier for this structure.</summary>
    public string Id { get; }

    /// <summary>Gets the world-space centre position of this structure, in metres.</summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Gets the half-extents of the axis-aligned bounding box, in metres.
    /// The full width along each axis is <c>2 * HalfExtents.X</c>, etc.
    /// </summary>
    public Vector3 HalfExtents { get; }

    /// <summary>
    /// Gets or sets the current damage state of this structure.
    /// Defaults to <see cref="DamageState.Intact"/> when the structure is created.
    /// </summary>
    public DamageState DamageState { get; set; }

    /// <summary>
    /// Initializes a new <see cref="Structure"/> at the given position and extents.
    /// </summary>
    /// <param name="id">A non-null, non-whitespace identifier that uniquely names this structure.</param>
    /// <param name="position">World-space centre position in metres.</param>
    /// <param name="halfExtents">Half-extents of the axis-aligned bounding box in metres.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is <see langword="null"/> or whitespace.
    /// </exception>
    public Structure(string id, Vector3 position, Vector3 halfExtents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        Id = id;
        Position = position;
        HalfExtents = halfExtents;
        DamageState = DamageState.Intact;
    }
}
