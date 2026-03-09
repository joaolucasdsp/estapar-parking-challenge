# Copilot Instructions — EstaparParkingChallenge

## Goal
Keep all generated code aligned with this repository's conventions and architecture.

## Stack
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core with provider strategy (PostgreSQL / SQL Server)
- MSTest for tests

## Mandatory Conventions
- Follow `.editorconfig` exactly.
- Private members must be `camelCase`.
- Non-private members must be `PascalCase`.
- Private async methods must be `camelCase` and end with `Async`.
- Interfaces must start with `I`.
- Use tabs for indentation in `*.cs` files.

## Architecture Rules
- Keep startup wiring in `Site/Program.cs` minimal and explicit.
- Register dependencies in extension classes (existing pattern: `AddAppServices`, `AddAppConfiguration`, `AddDatabaseProvider`).
- Database provider-specific EF configuration must stay inside `Site/DataStorage/*DatabaseProvider.cs`.
- Keep design-time EF logic inside `Site/DataStorage/Factories`.
- Reuse `AppDbContext` abstraction with provider-specific contexts.

## API Rules
- Prefer `[ApiController]` behavior for model validation.
- Keep global exception handling centralized in `HandleExceptionFilter` unless replacing with middleware by explicit request.
- Return consistent error payloads via existing `ApiException` + `ErrorModel` pattern.

## Naming Rules
- Use clear domain names; avoid vague names like `Helper`, `Manager`, `Utils` unless truly generic.
- Keep suffixes consistent:
  - `*Service` for business/application services
  - `*Config` for config binding models
  - `*DbContextFactory` for EF design-time factories
- New enums should use explicit meaningful member names and avoid abbreviations.

## Testing Rules
- Add/update tests when behavior changes.
- Prefer focused integration tests in `Tests/` using existing `WebApplicationFactory<Program>` pattern.
- Do not introduce a second test framework.

## Change Scope
- Make surgical changes only.
- Do not refactor unrelated files.
- Do not introduce new architectural patterns without explicit request.

## Output Quality
- Produce compile-ready code.
- Keep comments concise and only where they add real intent.
- Prefer consistency with existing code over personal style preferences.
