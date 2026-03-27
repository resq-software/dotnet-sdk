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

using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ResQ.Mavlink.Mesh.Tests;

/// <summary>
/// Unit tests for <see cref="MeshNeighborTable"/>.
/// </summary>
public sealed class MeshNeighborTableTests
{
    private static MeshNeighborTable CreateTable()
        => new(Options.Create(new MeshNeighborTableOptions()));

    [Fact]
    public void Update_NewNeighbor_AppearsInGetNeighbors()
    {
        var table = CreateTable();
        table.Update(2, -70, false);
        table.GetNeighbors().Should().ContainSingle(n => n.SystemId == 2);
    }

    [Fact]
    public void Update_ExistingNeighbor_RefreshesFields()
    {
        var table = CreateTable();
        table.Update(3, -80, false);
        table.Update(3, -65, true);

        var entry = table.GetNeighbors().Should().ContainSingle(n => n.SystemId == 3).Subject;
        entry.Rssi.Should().Be(-65);
        entry.HasGroundLink.Should().BeTrue();
    }

    [Fact]
    public void GetNeighbors_ExcludesStaleEntries()
    {
        var table = CreateTable();
        table.Update(5, -60, false);

        // Manually back-date the last seen timestamp
        var entry = table.GetAllNeighbors().Should().ContainSingle().Subject;
        entry.LastSeen = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(15);

        table.GetNeighbors().Should().BeEmpty("entry is stale");
        table.GetAllNeighbors().Should().HaveCount(1, "stale entries included in GetAllNeighbors");
    }

    [Fact]
    public void IsPartitioned_NoNeighborWithGroundLink_ReturnsTrue()
    {
        var table = CreateTable();
        table.Update(10, -70, false);
        table.Update(11, -72, false);
        table.IsPartitioned.Should().BeTrue();
    }

    [Fact]
    public void IsPartitioned_NeighborHasGroundLink_ReturnsFalse()
    {
        var table = CreateTable();
        table.Update(10, -70, false);
        table.Update(11, -72, hasGroundLink: true);
        table.IsPartitioned.Should().BeFalse();
    }

    [Fact]
    public void IsPartitioned_EmptyTable_ReturnsTrue()
    {
        var table = CreateTable();
        table.IsPartitioned.Should().BeTrue();
    }

    [Fact]
    public void BuildTopologyMessage_CorrectFields()
    {
        var table = CreateTable();
        table.Update(2, -60, false);
        table.Update(3, -70, true);

        var topo = table.BuildTopologyMessage(ownSystemId: 1);

        topo.ReporterSystemId.Should().Be(1);
        topo.NeighborCount.Should().Be(2);
        topo.HasGroundLink.Should().Be(1, "a neighbour with ground link present");
    }

    [Fact]
    public void BuildTopologyMessage_MaxFiveNeighbors()
    {
        var table = CreateTable();
        for (byte i = 2; i <= 9; i++) table.Update(i, -60 - i, false);

        var topo = table.BuildTopologyMessage(1);
        topo.NeighborCount.Should().Be(5);
    }
}
