---
name: testing
description: Testing rules for the ResQ .NET SDK.
---

# Testing Rules

## Framework

- Use xUnit for all tests. Moq for mocking.
- Test projects are named `<Package>.Tests` and live alongside their source project.

## Patterns

- Follow AAA (Arrange / Act / Assert) — one assertion concept per test method.
- Test method names: `MethodName_StateUnderTest_ExpectedBehavior`.
- Use `[Theory]` + `[InlineData]` / `[MemberData]` for parameterised cases.

## HTTP Mocking

- Never make real HTTP calls in unit tests. Use `MockHttpMessageHandler` (via `RichardSzalay.MockHttp`) or a custom `DelegatingHandler`.
- Integration tests may use `WebApplicationFactory` against an in-process test server.

## No Parallel Mutations

- Tests must not mutate shared static state. If a test sets environment variables, restore them in `IDisposable.Dispose()`.

## Coverage Gate

- New public API methods must have at least one unit test covering the happy path and one covering an error path.
