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
/// Configuration options for <see cref="UdpTransport"/>.
/// </summary>
public sealed class UdpTransportOptions
{
    /// <summary>Gets or sets the local UDP port to bind to. Defaults to 14550.</summary>
    public int ListenPort { get; set; } = 14550;

    /// <summary>Gets or sets the remote UDP port to send packets to. Defaults to 14550.</summary>
    public int RemotePort { get; set; } = 14550;

    /// <summary>Gets or sets the remote host address. Defaults to "127.0.0.1".</summary>
    public string RemoteHost { get; set; } = "127.0.0.1";

    /// <summary>Gets or sets the UDP receive buffer size in bytes. Defaults to 65535.</summary>
    public int ReceiveBufferSize { get; set; } = 65535;
}
