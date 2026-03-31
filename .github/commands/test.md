---
name: test
description: Run all xUnit tests and report results.
---

# /test

Run tests for the ResQ .NET SDK.

## Usage

```
/test [filter]
```

## Steps

1. Run `dotnet test --configuration Release --logger "console;verbosity=normal"` (or add `--filter <filter>` if given).
2. Report failed tests with test class, method, and failure message.
3. Report code coverage summary if `--collect:"XPlat Code Coverage"` is supported.
