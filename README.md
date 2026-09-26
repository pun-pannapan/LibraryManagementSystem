# Library Management System

A library management application built with ASP.NET Core 8, Angular 21, and SQL Server 2022.

The current code provides the local Docker setup, database migrations, sample book data, and an API status page. Authentication, book management, borrowing/returns, and Swagger are not implemented yet.

## Run Locally

Install Docker Desktop with the WSL2 backend and Linux containers enabled. Docker Compose is included; host installations of .NET, Node.js, and SQL Server are not needed to run the stack.

From PowerShell:

```powershell
git clone https://github.com/pun-pannapan/LibraryManagementSystem.git
cd LibraryManagementSystem
Copy-Item .env.example .env
```

Edit `.env` and replace the example passwords and JWT key before starting:

```powershell
docker compose up --build -d
docker compose ps
```

Wait for `db`, `api`, and `web` to show `healthy`.

| Service | Address |
| --- | --- |
| Frontend | http://localhost:4200 |
| API health | http://localhost:8080/health |
| API health through Nginx | http://localhost:4200/api/v1/health |
| SQL Server | 127.0.0.1,1433 |

These are the default ports. All published ports bind to `127.0.0.1`.

Stop the stack with:

```powershell
docker compose down
```

This removes the containers but keeps the database volume.

## Project Layout

| Path | Contents |
| --- | --- |
| `backend/src/LibraryManagement.Api/` | API, EF Core models, migrations, and seed data |
| `backend/tests/LibraryManagement.Api.Tests/` | Database configuration tests |
| `frontend/src/` | Angular application and component tests |
| `frontend/nginx.conf` | Static file serving and API proxy |
| `docker-compose.yml` | Local services, network, volume, and health checks |
| `.env.example` | Local configuration template |

## Configuration

Compose reads settings from the root `.env`. Keep that file local; only `.env.example` belongs in Git.

| Setting | Purpose |
| --- | --- |
| `MSSQL_SA_PASSWORD` | SQL Server login password used by the local API |
| `MSSQL_DATABASE` | Application database name |
| `ASPNETCORE_ENVIRONMENT` | `Development` enables startup migrations and sample data |
| `DB_PORT`, `API_PORT`, `WEB_PORT` | Published host ports |
| `CORS_ALLOWED_ORIGINS` | Comma-separated browser origins allowed by the API |
| `JWT_*`, `SEED_*` | Reserved for authentication and account seeding; not used yet |

Changing `MSSQL_SA_PASSWORD` in `.env` does not change the password in an existing SQL Server volume. Update the database login as well, or reset the volume if its data is disposable.

The API builds its connection string from `Database__Server`, `Database__Name`, `Database__User`, and `Database__Password`, which Compose supplies. The same helper is used by EF tooling and the database health check. It preserves passwords containing semicolons and quotes.

An explicit `ConnectionStrings__DefaultConnection` overrides those settings. Clear it from your shell when switching to the separate settings.

This is a development configuration: the API uses the `sa` login, SQL transport uses `Encrypt=False`, and `TrustServerCertificate=True` is set. Deployment needs separate credentials and transport configuration.

## Database

In Development, API startup applies pending migrations and seeds three categories and three books. Existing seed records are not duplicated on subsequent starts. The schema contains `Categories`, `Books`, `BorrowTransactions`, and `__EFMigrationsHistory`.

SQL Server stores data in the Compose volume `sqlserver-data`, mounted at `/var/opt/mssql`. Data survives container restarts and normal `docker compose down`.

### Migrations

Host-side migration commands require the .NET 8 SDK. Restore the repository-local EF tool:

```powershell
dotnet tool restore
```

Set these values from your local configuration. Use `localhost` and `DB_PORT` when connecting from the host, rather than the Docker service name `db`. EF tooling does not load `.env` automatically.

```powershell
$env:Database__Server = 'localhost,1433'
$env:Database__Name = 'LibraryManagementDb'
$env:Database__User = 'sa'
$env:Database__Password = '<your local MSSQL_SA_PASSWORD>'
$env:Cors__AllowedOrigins = 'http://localhost:4200'
```

PowerShell single-quoted strings preserve literal `$` characters. Double any single quote inside the password.

With the database running, apply migrations:

```powershell
dotnet tool run dotnet-ef database update --project backend/src/LibraryManagement.Api --startup-project backend/src/LibraryManagement.Api
```

To add a migration after changing the model, replace `MigrationName` with a descriptive name:

```powershell
dotnet tool run dotnet-ef migrations add MigrationName --project backend/src/LibraryManagement.Api --startup-project backend/src/LibraryManagement.Api --output-dir Migrations
```

### Reset

**This deletes all local database data.**

```powershell
docker compose down -v
docker compose up --build -d
```

With `ASPNETCORE_ENVIRONMENT=Development`, startup recreates the database and sample data.

## Development

Rebuild the API after changes:

```powershell
docker compose up --build -d api
```

Nginx forwards `/api/` to the API over `lms-network`. It uses Docker DNS to refresh the upstream address when the API container is replaced. Cached DNS may take about 10 seconds to expire; the configuration requires Nginx 1.27.3 or newer.

For Angular hot reload, follow the [frontend instructions](frontend/README.md). Stop the Compose `web` service to free port `4200`, and keep `db` and `api` running.

## Tests

Host-side tests require the .NET 8 SDK and Node.js 22.12 or newer within the Node 22 release line.

From the repository root:

```powershell
dotnet test backend/LibraryManagement.sln
npm --prefix frontend ci
npm --prefix frontend test -- --watch=false
```

Backend tests cover connection-string escaping, required settings, and the explicit connection-string override. Frontend tests cover the API status display for successful requests, HTTP errors, and network errors. These tests do not require Docker. There is no automated end-to-end suite yet.

## Health and Logs

Startup waits for SQL Server, then the API, then the web container. SQL Server runs a query for its health check; the API checks the application database connection; Nginx checks that it can serve the frontend.

```powershell
curl.exe http://localhost:8080/health
curl.exe http://localhost:4200/api/v1/health
```

Both endpoints return HTTP 200 with `status: Healthy` when the database is reachable. The response includes a `database` check and its duration in milliseconds. If the database check fails, the API returns HTTP 503.

Inspect recent logs or follow them:

```powershell
docker compose logs --tail=100 api
docker compose logs -f db
docker compose logs -f api
docker compose logs -f web
```

Ctrl+C stops following logs without stopping the containers. Docker captures stdout/stderr with the `local` logging driver, limited to three 10 MB log files per container.

If Compose reports `no configuration file provided`, run the command from the repository root. If a port is occupied, change it in `.env` and recreate the affected service. For other startup failures, check the service logs before resetting the database.
