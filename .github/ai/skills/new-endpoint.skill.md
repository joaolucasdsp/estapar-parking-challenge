# Skill: Add New API Endpoint (Repository Standard)

## Objective
Implement a new endpoint without breaking repository conventions.

## Checklist
1. Add request/response contracts in `Api/` when they are shared/public.
2. Implement endpoint in `Site/Controllers` using `[ApiController]` conventions.
3. Put business logic in `Site/Services` (`*Service`) and keep controller thin.
4. Use dependency injection through existing registration extensions.
5. If persistence is needed, use `AppDbContext` and preserve provider-agnostic behavior.
6. For custom domain errors, throw `ApiException` with `ErrorCodes`.
7. Add/update integration tests under `Tests/`.

## Naming Requirements
- Public methods: `PascalCase`
- Private methods/fields/properties: `camelCase`
- Async methods:
  - public/protected/internal: `PascalCase` + `Async`
  - private: `camelCase` + `Async`

## Do Not
- Do not add new frameworks.
- Do not add alternative validation filters if `[ApiController]` behavior is enough.
- Do not move migration strategy away from per-provider folders.
