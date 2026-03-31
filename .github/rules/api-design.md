---
name: api-design
description: Public API design rules for the ResQ .NET SDK packages.
---

# API Design Rules

## Async

- All I/O-bound operations must be `async Task<T>` — never block with `.Result` or `.GetAwaiter().GetResult()`.
- Every async method must accept a `CancellationToken cancellationToken = default` parameter as the last parameter.
- Method names must end in `Async` (e.g., `GetDroneStatusAsync`).

## Nullability

- `<Nullable>enable</Nullable>` is required in all projects.
- Public APIs must never return `null` where a non-null type is declared. Use `Option`-style wrappers or throw meaningful exceptions.
- Input parameters must be validated: throw `ArgumentNullException` for null references, `ArgumentException` for invalid values.

## Immutability

- DTOs (models) are records or have `init`-only setters.
- Configuration option classes use `init` setters and are sealed.
- Collections returned from APIs are `IReadOnlyList<T>` or `IReadOnlyDictionary<K,V>` — never raw `List<T>`.

## Versioning

- Breaking changes require a major version bump (MinVer + git tag).
- Deprecations must use `[Obsolete("Use X instead.", error: false)]` and survive one minor version before removal.
- New optional parameters must be added at the end with defaults — never change existing parameter order.

## Documentation

- All public types and members must have XML `<summary>` doc comments.
- `<param>`, `<returns>`, and `<exception>` tags are required for public methods.
- Use `<see cref="..."/>` for cross-references — no bare type names in prose.
