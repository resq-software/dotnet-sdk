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

namespace ResQ.Mavlink.Transport;

/// <summary>
/// Exception thrown when an operation is attempted on a disconnected or disposed transport.
/// </summary>
public sealed class TransportDisconnectedException : InvalidOperationException
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public TransportDisconnectedException()
        : base("The MAVLink transport is disconnected or disposed.")
    {
    }

    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">The error message.</param>
    public TransportDisconnectedException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TransportDisconnectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
