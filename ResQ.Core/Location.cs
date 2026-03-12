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

namespace ResQ.Core;

/// <summary>
/// Represents a geographic location with latitude, longitude, and optional altitude.
/// </summary>
/// <remarks>
/// This record provides a standard way to represent geographic coordinates throughout
/// the ResQ system. It includes a method to calculate distance between locations
/// using the Haversine formula for great-circle distance.
/// </remarks>
/// <param name="Latitude">Latitude in decimal degrees (-90 to 90).</param>
/// <param name="Longitude">Longitude in decimal degrees (-180 to 180).</param>
/// <param name="Altitude">Altitude in meters above sea level (optional).</param>
/// <example>
/// <code>
/// var location = new Location(37.7749, -122.4194); // San Francisco
/// var locationWithAlt = new Location(37.7749, -122.4194, 100.5);
///
/// // Calculate distance between two locations
/// var sf = new Location(37.7749, -122.4194);
/// var la = new Location(34.0522, -118.2437);
/// double distanceKm = sf.DistanceTo(la); // ~559 km
/// </code>
/// </example>
public record Location(
    double Latitude,
    double Longitude,
    double? Altitude = null
)
{
    /// <summary>
    /// Calculates the great-circle distance to another location using the Haversine formula.
    /// </summary>
    /// <param name="other">The target location to calculate distance to.</param>
    /// <returns>The distance in kilometers between this location and the target.</returns>
    /// <remarks>
    /// This method uses the Haversine formula to calculate the shortest distance over
    /// the earth's surface, giving an "as-the-crow-flies" distance between the points
    /// (ignoring any hills, valleys, or other obstacles). The calculation assumes
    /// a spherical earth with a radius of 6,371 kilometers.
    /// </remarks>
    /// <example>
    /// <code>
    /// var point1 = new Location(37.7749, -122.4194); // San Francisco
    /// var point2 = new Location(34.0522, -118.2437); // Los Angeles
    ///
    /// double distance = point1.DistanceTo(point2);
    /// Console.WriteLine($"Distance: {distance:F2} km"); // Distance: 559.12 km
    /// </code>
    /// </example>
    public double DistanceTo(Location other)
    {
        const double R = 6371.0; // Earth radius in km
        var lat1Rad = Latitude * Math.PI / 180.0;
        var lat2Rad = other.Latitude * Math.PI / 180.0;
        var deltaLat = (other.Latitude - Latitude) * Math.PI / 180.0;
        var deltaLon = (other.Longitude - Longitude) * Math.PI / 180.0;

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}

/// <summary>
/// Represents a velocity vector in NED (North-East-Down) coordinate frame.
/// </summary>
/// <remarks>
/// The NED frame is a local tangent plane coordinate system commonly used in aviation
/// and robotics. Positive X is North, positive Y is East, and positive Z is Down.
/// </remarks>
/// <param name="Vx">Velocity in North direction (m/s). Positive is North, negative is South.</param>
/// <param name="Vy">Velocity in East direction (m/s). Positive is East, negative is West.</param>
/// <param name="Vz">Velocity in Down direction (m/s). Positive is Down, negative is Up.</param>
/// <example>
/// <code>
/// // Drone moving North at 10 m/s, East at 5 m/s, and climbing at 2 m/s
/// var velocity = new Velocity(10.0, 5.0, -2.0);
///
/// // Calculate ground speed (ignoring altitude change)
/// double groundSpeed = Math.Sqrt(velocity.Vx * velocity.Vx + velocity.Vy * velocity.Vy);
/// </code>
/// </example>
public record Velocity(double Vx, double Vy, double Vz);
