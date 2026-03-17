# NuGet Package README Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Embed a shared README in every published `ResQ.*` NuGet package.

**Architecture:** Add a package-focused markdown file at repo root and wire it globally in `Directory.Build.props` so all packable projects include it during `dotnet pack`.

**Tech Stack:** MSBuild, NuGet packaging metadata, .NET 9 SDK

---

## Chunk 1: Red/Green Packaging Verification

### Task 1: Capture and remove the missing README warning

**Files:**
- Create: `NUGET_README.md`
- Modify: `Directory.Build.props`

- [ ] **Step 1: Run the failing package verification**

Run `dotnet pack` and confirm the current output warns that the generated packages are missing a readme.

- [ ] **Step 2: Add the shared package README**

Create a concise markdown file oriented toward NuGet consumers.

- [ ] **Step 3: Wire global packaging metadata**

Set `PackageReadmeFile` and include the markdown file in pack output through `Directory.Build.props`.

- [ ] **Step 4: Re-run `dotnet pack`**

Confirm packages are created without the missing README warning.

- [ ] **Step 5: Commit**

Commit the packaging metadata change after verification.
