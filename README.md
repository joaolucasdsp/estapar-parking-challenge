# Estapar Parking Challenge

Backend solution for the Estapar technical challenge using ASP.NET Core 10 Web API. Includes multi-provider database support, webhook processing, dynamic pricing, revenue API, OpenAPI docs, and optional Redis caching.

## Features

- Runtime: .NET 10 / ASP.NET Core
- Database: Entity Framework Core 10 (PostgreSQL or SQL Server)
- Authentication: JWT Bearer
- API docs: Scalar + OpenAPI
- Cache: Redis (optional)
- Logging: Serilog + New Relic
- Tests: integration tests project

## Prerequisites

- .NET 10 SDK
- PostgreSQL or SQL Server (or Docker)
- dotnet-ef CLI

```sh
dotnet tool install --global dotnet-ef
```

## Project Structure

- `EstaparParkingChallenge.Site/`: Web API (controllers, services, EF contexts, migrations)
- `EstaparParkingChallenge.Api/`: shared contracts (models, errors, pagination)
- `EstaparParkingChallenge.Tests/`: automated tests
  - `Tests/Unit/`: unit tests
  - `Tests/Integration/`: integration tests (real database configured in `appsettings.Test.json`)

## Configuration

Update `Site/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "PostgreSqlConnection": "Server=localhost;Database=EstaparParkingChallenge;...",
    "SqlServerConnection": "Server=localhost;Database=EstaparParkingChallenge;..."
  },
  "Jwt": {
    "Secret": "<base64-secret>",
    "Issuer": "EstaparParkingChallenge.local"
  },
  "Redis": {
    "Enabled": false,
    "ConnectionString": "localhost:6379"
  },
  "Startup": {
    "Database": 1,
    "ApplyMigrations": false
  }
}
```

- `Startup.Database`: `1` = PostgreSQL, `2` = SQL Server

## Run Locally

```sh
dotnet restore EstaparParkingChallenge.sln
dotnet run --project Site/EstaparParkingChallenge.Site.csproj -lp EstaparParkingChallenge.Site
```

- Scalar: `http://localhost:5139/scalar/v1`
- OpenAPI: `http://localhost:5139/openapi/v1.json`
- Health check: `GET /api/health`

## Generate Application Token

```sh
dotnet run --project Site/EstaparParkingChallenge.Site.csproj -- --gen-app-token -n MyApplication
```

## Migrations

Folders:

- PostgreSQL: `Site/Migrations/Postgres`
- SQL Server: `Site/Migrations/SqlServer`

Create migration (PostgreSQL):

```sh
dotnet ef migrations add <MigrationName> \
  --project Site/EstaparParkingChallenge.Site.csproj \
  --startup-project Site/EstaparParkingChallenge.Site.csproj \
  --context PostgresDbContext \
  --output-dir Migrations/Postgres
```

Create migration (SQL Server):

```sh
dotnet ef migrations add <MigrationName> \
  --project Site/EstaparParkingChallenge.Site.csproj \
  --startup-project Site/EstaparParkingChallenge.Site.csproj \
  --context SqlServerDbContext \
  --output-dir Migrations/SqlServer
```

Apply migrations:

```sh
dotnet ef database update \
  --project Site/EstaparParkingChallenge.Site.csproj \
  --startup-project Site/EstaparParkingChallenge.Site.csproj \
  --context PostgresDbContext

dotnet ef database update \
  --project Site/EstaparParkingChallenge.Site.csproj \
  --startup-project Site/EstaparParkingChallenge.Site.csproj \
  --context SqlServerDbContext
```

If preferred, set `Startup.ApplyMigrations` to `true` for automatic migration on startup.

## Tests

Run unit tests only:

```sh
dotnet test ./Tests/EstaparParkingChallenge.Tests.csproj --filter "TestCategory=Unit"
```

Run integration tests only (uses `ASPNETCORE_ENVIRONMENT=Test` and configured database):

```sh
dotnet test ./Tests/EstaparParkingChallenge.Tests.csproj --filter "TestCategory=Integration"
```

Run all tests:

```sh
dotnet test ./Tests/EstaparParkingChallenge.Tests.csproj
```

## Docker

```sh
docker-compose up --build
```

With load balancer:

```sh
docker compose -f docker-compose-load-balancer.yml up --build
```
