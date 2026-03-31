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
/// Configuration options for <see cref="SerialTransport"/>.
/// </summary>
public sealed class SerialTransportOptions
{
    /// <summary>Gets or sets the serial port name. Default is <c>"/dev/ttyUSB0"</c>.</summary>
    public string PortName { get; set; } = "/dev/ttyUSB0";

    /// <summary>Gets or sets the baud rate. Default is <c>57600</c>.</summary>
    public int BaudRate { get; set; } = 57600;

    /// <summary>Gets or sets the read timeout. Default is 2 seconds.</summary>
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(2);
}
