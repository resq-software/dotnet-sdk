# NuGet Package README Design

## Goal

Embed a README file in every published `ResQ.*` NuGet package so the current NuGet warning disappears and package pages on nuget.org display useful consumer-facing documentation.

## Context

The repository already publishes multiple packages from a shared release pipeline. Current package metadata sets description, license, and repository URL, but it does not set `PackageReadmeFile` or include a markdown file in the `.nupkg`.

## Decision

Use one shared package README for all published packages.

## Rationale

- all published packages come from the same SDK repository
- a single readme keeps the packaging metadata centralized
- `Directory.Build.props` already provides shared packaging defaults, so it is the correct place to wire the README globally

## Design

- add a new repo-root `NUGET_README.md` focused on package consumers
- set `PackageReadmeFile` in `Directory.Build.props`
- include `NUGET_README.md` in package output for packable projects through shared MSBuild metadata
- verify with `dotnet pack` that the previous README warning is gone

## Testing

- run `dotnet pack` before the change and observe the missing README warning
- run `dotnet pack` after the change and confirm packages are created without the warning
