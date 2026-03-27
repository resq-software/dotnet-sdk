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

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace ResQ.Mavlink.Sitl;

/// <summary>
/// Configuration options for <see cref="SitlProcessManager"/>.
/// </summary>
public sealed class SitlProcessManagerOptions
{
    /// <summary>
    /// Gets or sets the full path to the ArduPilot SITL binary (e.g. <c>/usr/local/bin/arducopter</c>).
    /// </summary>
    public string SitlBinaryPath { get; set; } = "arducopter";

    /// <summary>
    /// Gets or sets the base MAVLink TCP/UDP port. Instance <c>i</c> uses port <c>BasePort + i * 10</c>.
    /// Defaults to <c>5760</c>.
    /// </summary>
    public int BasePort { get; set; } = 5760;

    /// <summary>
    /// Gets or sets the base JSON physics interface port.
    /// Instance <c>i</c> uses port <c>BaseJsonPort + i</c>.
    /// Defaults to <c>9002</c>.
    /// </summary>
    public int BaseJsonPort { get; set; } = 9002;

    /// <summary>
    /// Gets or sets the maximum number of simultaneous SITL instances.
    /// Defaults to <c>20</c>.
    /// </summary>
    public int MaxInstances { get; set; } = 20;

    /// <summary>
    /// Gets or sets the home location in "lat,lon,alt,heading" format passed to ArduPilot's
    /// <c>--home</c> argument. Defaults to a generic test location.
    /// </summary>
    public string HomeLocation { get; set; } = "-35.363262,149.165237,584,353";

    /// <summary>
    /// Gets or sets the SITL vehicle model (e.g. <c>"+"</c>, <c>"quad"</c>, <c>"json"</c>).
    /// Defaults to <c>"+"</c>.
    /// </summary>
    public string Model { get; set; } = "+";
}

/// <summary>
/// Manages one or more ArduPilot SITL child processes, providing lifecycle management
/// (spawn / graceful kill) and port allocation helpers.
/// </summary>
public sealed class SitlProcessManager : IAsyncDisposable
{
    private readonly SitlProcessManagerOptions _options;
    private readonly ConcurrentDictionary<int, Process> _processes = new();
    private bool _disposed;

    /// <summary>
    /// Initialises a new <see cref="SitlProcessManager"/> using <see cref="IOptions{T}"/> for DI.
    /// </summary>
    /// <param name="options">Manager options wrapped in IOptions.</param>
    public SitlProcessManager(IOptions<SitlProcessManagerOptions> options)
        : this(options.Value)
    {
    }

    /// <summary>
    /// Initialises a new <see cref="SitlProcessManager"/> with raw options.
    /// </summary>
    /// <param name="options">Manager options.</param>
    public SitlProcessManager(SitlProcessManagerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Returns the MAVLink port for the given <paramref name="instanceIndex"/>.
    /// </summary>
    /// <param name="instanceIndex">Zero-based instance index.</param>
    /// <returns>The UDP/TCP port number for MAVLink communication.</returns>
    public int GetMavlinkPort(int instanceIndex) => _options.BasePort + instanceIndex * 10;

    /// <summary>
    /// Returns the JSON physics interface port for the given <paramref name="instanceIndex"/>.
    /// </summary>
    /// <param name="instanceIndex">Zero-based instance index.</param>
    /// <returns>The UDP port number for the JSON physics bridge.</returns>
    public int GetJsonPort(int instanceIndex) => _options.BaseJsonPort + instanceIndex;

    /// <summary>
    /// Spawns a new ArduPilot SITL process for the specified <paramref name="instanceIndex"/>.
    /// </summary>
    /// <param name="instanceIndex">
    /// Zero-based index in the range [0, <see cref="SitlProcessManagerOptions.MaxInstances"/>).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The spawned <see cref="Process"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="instanceIndex"/> is out of range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a process for this instance is already running.
    /// </exception>
    public Task<Process> SpawnAsync(int instanceIndex, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (instanceIndex < 0 || instanceIndex >= _options.MaxInstances)
            throw new ArgumentOutOfRangeException(nameof(instanceIndex),
                $"Instance index must be in [0, {_options.MaxInstances}).");

        if (_processes.ContainsKey(instanceIndex))
            throw new InvalidOperationException(
                $"A SITL process for instance {instanceIndex} is already running.");

        var mavPort = GetMavlinkPort(instanceIndex);
        var jsonPort = GetJsonPort(instanceIndex);

        var args = string.Join(' ',
            $"--model {_options.Model}",
            $"--home {_options.HomeLocation}",
            $"--instance {instanceIndex}",
            $"--serial0 udp:{mavPort}",
            $"--sim-port-in {jsonPort}");

        var psi = new ProcessStartInfo(_options.SitlBinaryPath, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        var process = new Process { StartInfo = psi };
        process.Start();

        _processes[instanceIndex] = process;

        return Task.FromResult(process);
    }

    /// <summary>
    /// Gracefully terminates the SITL process for the given <paramref name="instanceIndex"/>.
    /// If the process does not exit within a short timeout, it is forcibly killed.
    /// </summary>
    /// <param name="instanceIndex">Zero-based instance index.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task KillAsync(int instanceIndex, CancellationToken ct = default)
    {
        if (!_processes.TryGetValue(instanceIndex, out var process))
            return;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(ct).ConfigureAwait(false);
            }
        }
        catch (InvalidOperationException)
        {
            // Process may have already exited.
        }
        finally
        {
            process.Dispose();
            _processes.TryRemove(instanceIndex, out _);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        var indices = _processes.Keys.ToArray();
        foreach (var idx in indices)
        {
            await KillAsync(idx).ConfigureAwait(false);
        }
    }
}
