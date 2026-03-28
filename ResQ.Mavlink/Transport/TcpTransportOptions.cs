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
/// Configuration options for <see cref="TcpTransport"/>.
/// </summary>
public sealed class TcpTransportOptions
{
    /// <summary>Gets or sets the remote host to connect to (client mode) or the local bind address (server mode). Default is <c>"127.0.0.1"</c>.</summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>Gets or sets the TCP port. Default is <c>5760</c>.</summary>
    public int Port { get; set; } = 5760;

    /// <summary>Gets or sets the delay between reconnect attempts. Default is 2 seconds.</summary>
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Gets or sets the maximum number of packets to buffer during reconnect. <c>0</c> disables buffering. Default is <c>100</c>.</summary>
    public int MaxReconnectBuffer { get; set; } = 100;

    /// <summary>Gets or sets whether the transport acts as a server (listens for one connection) rather than a client. Default is <c>false</c>.</summary>
    public bool IsServer { get; set; } = false;
}
