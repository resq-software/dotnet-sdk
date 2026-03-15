---
name: pack
description: Pack all SDK packages into .nupkg artifacts for publishing.
---

# /pack

Pack the ResQ .NET SDK NuGet packages.

## Steps

1. Run `dotnet pack --configuration Release --output ./artifacts`.
2. List generated `.nupkg` files with their version numbers.
3. Verify each of the 6 expected packages is present.
4. Do NOT push to NuGet.org — that is a CI-only step triggered by a git tag.
