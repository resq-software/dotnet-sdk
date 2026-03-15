---
name: build
description: Build all SDK packages in Release configuration.
---

# /build

Build the ResQ .NET SDK.

## Steps

1. Run `dotnet build --configuration Release`.
2. Report any warnings (treat warnings as errors in CI — `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).
3. Confirm all 6 projects compiled successfully.
