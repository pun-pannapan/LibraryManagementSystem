# Library Management System

A library management application built with ASP.NET Core 8, Angular 21, and SQL Server 2022.

## Prerequisites

- Docker Desktop with WSL2 backend and Linux containers enabled
- .NET 8 SDK, if you want to build, test, or run EF commands for the backend from the host
- Node.js 22.12 or newer in the Node 22 release line, if you want to build, test, or run the frontend from the host

## Run Locally

From PowerShell:

```powershell
git clone https://github.com/pun-pannapan/LibraryManagementSystem.git
cd LibraryManagementSystem
Copy-Item .env.example .env
```

Edit `.env` before starting the stack. Replace the example SQL Server passwords, JWT key, and seed user passwords.

### Build and start the full solution with Docker

Build all images and start the database, backend API, and frontend:

```powershell
docker compose up --build -d
docker compose ps
```

Wait until `db`, `api`, and `web` show `healthy`.

| Service | Address |
| --- | --- |
| Frontend | http://localhost:4200 |
| API health | http://localhost:8080/health |
| API through frontend proxy | http://localhost:4200/api/v1/health |
| Swagger | http://localhost:8080/swagger |
| SQL Server | 127.0.0.1,1433 |

Stop the stack:

```powershell
docker compose down
```

This stops containers but keeps the SQL Server volume.

### Build from the host

Use these commands when you want to verify or develop the backend and frontend outside their containers. Run them from the repository root.

Build the .NET solution:

```powershell
dotnet restore backend/LibraryManagement.sln
dotnet build backend/LibraryManagement.sln --configuration Release
```

Install dependencies and build the Angular frontend:

```powershell
npm --prefix frontend ci
npm --prefix frontend run build
```

Build both Docker images without starting them:

```powershell
docker compose build
```

To run the frontend with the Angular development server while the database and API stay in Docker:

```powershell
docker compose up --build -d db api
npm --prefix frontend start
```

Open <http://localhost:4200>. The development server proxies `/api/**` to the API at `http://127.0.0.1:8080`.

## Configuration

Docker Compose reads settings from `.env` in the repository root.

| Setting | Purpose |
| --- | --- |
| `MSSQL_SA_PASSWORD` | SQL Server `sa` password |
| `MSSQL_DATABASE` | Application database name |
| `ASPNETCORE_ENVIRONMENT` | Use `Development` for local migrations, seed data, and Swagger |
| `DB_PORT`, `API_PORT`, `WEB_PORT` | Host ports |
| `CORS_ALLOWED_ORIGINS` | Allowed browser origins |
| `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_KEY`, `JWT_EXPIRY_MINUTES` | JWT settings |
| `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD` | Local admin account |
| `SEED_USER_EMAIL`, `SEED_USER_PASSWORD` | Local user account |

Changing `MSSQL_SA_PASSWORD` after the database volume already exists does not change the password stored inside SQL Server. Update the existing login manually or reset the volume if the data is disposable.

## Demo Accounts

The local development seed creates demo accounts from `.env`.

| Role | Default email |
| --- | --- |
| Administrator | `admin@example.com` |
| User | `user@example.com` |

These accounts are for local development only. Change the passwords before sharing an environment.

## API

Swagger is available in Development:

```text
http://localhost:8080/swagger
```

Main endpoints:

```http
POST /api/v1/auth/register
POST /api/v1/auth/login
GET  /api/v1/auth/me

GET    /api/v1/categories
GET    /api/v1/books
GET    /api/v1/books/{id}
POST   /api/v1/books
PUT    /api/v1/books/{id}
DELETE /api/v1/books/{id}

POST /api/v1/borrowings
POST /api/v1/borrowings/{id}/return
POST /api/v1/borrowings/{id}/request-return
POST /api/v1/borrowings/{id}/cancel
POST /api/v1/borrowings/{id}/assign       (Administrator)
POST /api/v1/borrowings/{id}/reject       (Administrator)
POST /api/v1/borrowings/{id}/accept-return (Administrator)
GET  /api/v1/borrowings/me
GET  /api/v1/borrowings

GET /health
GET /api/v1/health
```

Book browsing, category listing, book create/update/delete, and borrowing endpoints require a valid JWT. Creating, updating, deleting books, and viewing full borrowing history additionally require an administrator token.

## Frontend manual test guide

Run this browser checklist first, after `docker compose up --build -d` has started the database, API, and web containers. Use the email and password values configured in `.env`.

### 1. Check the frontend is reachable

- Open <http://localhost:4200>.
- Confirm the Login page is displayed.
- Open browser DevTools → Network and keep it open while testing.

### 2. Test login validation

- Submit the Login form with both fields empty; required-field messages should appear and no API request should be sent.
- Enter an invalid email; the email validation message should appear before submission.
- Enter a valid email with the wrong password; the page should stay on `/login` and show an incorrect-credentials message.

### 3. Test User

Use the account configured by `SEED_USER_EMAIL` and `SEED_USER_PASSWORD` in `.env`.

- Login successfully and confirm that `/books` opens.
- Open a book detail page.
- Request an available book and confirm it becomes `Requested`.
- Open `/my-borrowings` and confirm the borrowing appears.
- As an administrator, open `/admin/transactions` and click `Assign`.
- As the user, request the book return and confirm it becomes `ReturnRequested`.
- As an administrator, click `Accept Return` and confirm the transaction becomes `Returned`.
- Confirm that administrator links are not visible.

### 4. Test Administrator

Use the account configured by `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD` in `.env`.

- Login successfully and open `/admin/books`.
- Create a book, edit it, and delete it.
- Set and verify the book Shelf Code and Location fields.
- Confirm that required-field, whitespace-only, invalid-year, and missing-category validation prevents invalid submissions.
- Open `/admin/transactions`.
- Search by a user email/name, book title, or ISBN, then test status, date, and pagination filters.
- Confirm the table shows the user email and book title without internal UUID values.

### 5. Check frontend-to-backend requests

In DevTools → Network, confirm that:

- Login calls `POST /api/v1/auth/login` and returns `200`.
- The book list calls `/api/v1/books`.
- Categories call `/api/v1/categories`.
- Authenticated requests include `Authorization: Bearer ...`.
- Unexpected `401` or `403` responses do not occur.

### 6. Check logs when a test fails

```powershell
docker compose logs --tail=100 api
docker compose logs --tail=100 web
docker compose logs --tail=100 db
```

## Backend API Test Guide

You can test the backend from Swagger at:

```text
http://localhost:8080/swagger
```

### 1. Login as Administrator

Call:

```http
POST /api/v1/auth/login
```

Request body:

```json
{
  "email": "admin@example.com",
  "password": "ChangeThisAdminPassword123!"
}
```

Copy the `token` value from the response. In Swagger, click `Authorize` and paste the token only. Do not include the `Bearer` prefix.

### 2. Create a Book

Call:

```http
POST /api/v1/books
```

Request body:

```json
{
  "isbn": "9780321125217",
  "title": "Domain-Driven Design",
  "author": "Eric Evans",
  "publisher": "Addison-Wesley",
  "publishedYear": 2003,
  "categoryId": "10000000-0000-0000-0000-000000000001"
}
```

Expected result:

```text
201 Created
```

Save the returned `id` and `rowVersion` if you want to test update.

### 3. Update a Book

First get the latest book data:

```http
GET /api/v1/books/{id}
```

Then call:

```http
PUT /api/v1/books/{id}
```

Request body:

```json
{
  "isbn": "9780321125217",
  "title": "Domain-Driven Design Updated",
  "author": "Eric Evans",
  "publisher": "Addison-Wesley",
  "publishedYear": 2003,
  "categoryId": "10000000-0000-0000-0000-000000000001",
  "rowVersion": "PASTE_LATEST_ROW_VERSION_HERE"
}
```

Expected result:

```text
200 OK
```

Use the latest `rowVersion` from `GET /api/v1/books/{id}`. If the row version is stale, the API returns `409 Conflict`.

### 4. Login as User

Call:

```http
POST /api/v1/auth/login
```

Request body:

```json
{
  "email": "user@example.com",
  "password": "ChangeThisUserPassword123!"
}
```

Authorize in Swagger with the returned user token.

### 5. Borrow a Book

Find an available book:

```http
GET /api/v1/books
```

Call:

```http
POST /api/v1/borrowings
```

Request body:

```json
{
  "bookId": "20000000-0000-0000-0000-000000000001"
}
```

Expected result:

```text
201 Created
```

Save the returned borrowing `id`.

### 6. View User Borrowing History

Call:

```http
GET /api/v1/borrowings/me
```

Optional query parameters:

```text
status=Borrowed
page=1
pageSize=20
sort=-requestedAt
```

### 7. Return a Book

Call:

```http
POST /api/v1/borrowings/{id}/return
```

No request body is required.

Expected result:

```text
200 OK
```

The return endpoint creates a `ReturnRequested` transaction. An administrator must call
`POST /api/v1/borrowings/{id}/accept-return` to complete the return and make the book available again.

### 8. Check Authorization Rules

With a normal user token, call:

```http
GET /api/v1/borrowings
```

Expected result:

```text
403 Forbidden
```

## Database

In Development, the API applies migrations and seeds the local database during startup.

`Categories.Id`, `Books.Id`, and `BorrowTransactions.Id` (plus the related book/category foreign keys) use SQL Server `uniqueidentifier` values. The `UseGuidLibraryEntityIds` migration preserves existing relationships while assigning GUIDs to existing rows. Because converting those generated values back to identity integers is not lossless, the migration's down direction is intentionally unsupported; use a database backup or `docker compose down -v` when a clean reset is required.

### High-Level Architecture

The system uses a layered web architecture. The browser hosts the Angular single-page application, while Nginx serves the built frontend and proxies API requests to the ASP.NET Core service. The API applies authentication, validation, and borrowing workflow rules before accessing SQL Server through EF Core and ASP.NET Core Identity.

[Architecture diagram image](assets/diagram/architech%20diagram.png)

![High-level architecture diagram](assets/diagram/architech%20diagram.png)

```mermaid
flowchart TB
    Browser["Web browser"]

    subgraph Docker["Docker Compose deployment"]
        subgraph WebContainer["web container"]
            Angular["Angular SPA<br/>Books, borrowings, admin"]
            Nginx["Nginx<br/>Static hosting + reverse proxy"]
        end

        subgraph ApiContainer["api container"]
            Api["ASP.NET Core 8 API<br/>REST endpoints"]
            Auth["JWT authentication<br/>Role-based authorization"]
            Contracts["DTOs / contracts"]
            Validation["Request validation"]
            Workflow["Borrowing workflow<br/>Request → Assign → Borrow → Return request → Accept"]
            Ef["EF Core persistence"]
            Identity["ASP.NET Core Identity"]
            Migrations["Migrations + seed data"]
        end

        subgraph DbContainer["db container"]
            Sql["SQL Server"]
        end
    end

    Decisions["Key decisions<br/>JWT + RBAC<br/>Server-side filtering, sorting, pagination<br/>Explicit borrowing state transitions<br/>Docker Compose deployment"]

    Browser --> Angular
    Angular -->|HTTPS JSON + bearer token| Nginx
    Nginx --> Api
    Api --> Auth
    Api --> Contracts
    Api --> Validation
    Api --> Workflow
    Workflow --> Ef
    Auth --> Identity
    Ef --> Sql
    Identity --> Sql
    Migrations --> Sql
    Decisions -.-> Api
    Decisions -.-> Ef
```

Data flows from the Angular UI to the API as authenticated JSON requests over the Docker Compose network. The API validates input and authorization, executes the relevant borrowing or inventory workflow, persists changes in the SQL Server container, and returns DTOs and HTTP status codes to the UI. Sorting, filtering, and pagination are performed server-side so each screen can request only the data it needs.

### ER Diagram

[ER diagram image](assets/diagram/er%20diagram.png)

![Entity relationship diagram](assets/diagram/er%20diagram.png)

```mermaid
erDiagram
    CATEGORIES ||--o{ BOOKS : contains
    BOOKS ||--o{ BORROW_TRANSACTIONS : has
    ASP_NET_USERS ||--o{ BORROW_TRANSACTIONS : creates

    CATEGORIES {
        guid Id PK
        string Name
        string Description
    }

    BOOKS {
        guid Id PK
        string Isbn UK
        string Title
        string Author
        string Publisher
        int PublishedYear
        string ShelfCode
        string Location
        int AvailabilityStatus
        guid CategoryId FK
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
        rowversion RowVersion
    }

    BORROW_TRANSACTIONS {
        guid Id PK
        guid BookId FK
        guid UserId FK
        datetime BorrowedAtUtc
        datetime DueAtUtc
        datetime ReturnedAtUtc
        datetime RequestedAtUtc
        datetime AssignedAtUtc
        datetime ReturnRequestedAtUtc
        guid AssignedByUserId
        guid ProcessedByUserId
        int Status
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
    }

    ASP_NET_USERS {
        guid Id PK
        string Email
        string UserName
        string FirstName
        string LastName
        bool IsActive
        datetime CreatedAtUtc
    }
```

SQL Server data is stored in the Docker volume `sqlserver-data`. To reset all local data:

```powershell
docker compose down -v
docker compose up --build -d
```

Host-side EF commands require the .NET 8 SDK and the local EF tool:

```powershell
dotnet tool restore
```

Set connection values before running EF commands from the host:

```powershell
$env:Database__Server = 'localhost,1433'
$env:Database__Name = 'LibraryManagementDb'
$env:Database__User = 'sa'
$env:Database__Password = '<your local MSSQL_SA_PASSWORD>'
$env:Cors__AllowedOrigins = 'http://localhost:4200'
$env:Jwt__Issuer = 'LibraryManagement'
$env:Jwt__Audience = 'LibraryManagement.Client'
$env:Jwt__Key = 'ChangeThisToAtLeast32Characters123'
```

Apply migrations:

```powershell
dotnet tool run dotnet-ef database update --project backend/src/LibraryManagement.Api --startup-project backend/src/LibraryManagement.Api
```

Add a migration:

```powershell
dotnet tool run dotnet-ef migrations add MigrationName --project backend/src/LibraryManagement.Api --startup-project backend/src/LibraryManagement.Api --output-dir Migrations
```

## Tests

Run backend restore, build, and tests:

```powershell
dotnet restore backend/LibraryManagement.sln
dotnet build backend/LibraryManagement.sln --configuration Release
dotnet test backend/LibraryManagement.sln
```

Run frontend tests and a production build:

```powershell
npm --prefix frontend ci
npm --prefix frontend test -- --watch=false
npm --prefix frontend run build
```

Run all automated checks before opening a pull request:

```powershell
dotnet test backend/LibraryManagement.sln
npm --prefix frontend test -- --watch=false
docker compose build
```

## Frontend

The frontend is a standalone Angular 21 application using Angular Router, Bootstrap 5, Signals, RxJS, Reactive Forms, and Vitest. Its API base URL is `/api/v1`; the Angular development server proxies `/api/**` to the local backend.

Main frontend routes:

- `/login` — sign in
- `/books` and `/books/:id` — browse books and view details
- `/my-borrowings` — view and return personal borrowings
- `/admin/books`, `/admin/books/new`, `/admin/books/:id/edit` — administrator inventory
- `/admin/transactions` — administrator transaction history
- `/forbidden` and `/not-found` — access and route fallback pages

## AI Usage

AI was used as a development assistant for reviewing, testing, and documenting this project. All generated suggestions were checked against the source code, API contracts, build output, and automated tests before being kept.

### AI Tools Disclosure

The project used **OpenAI Codex (ChatGPT-based coding assistant)** during development. It was used as a support tool when development issues occurred, including:

- Investigating errors from the frontend, backend API, Docker containers, and browser runtime
- Reviewing changed code for correctness, security, readability, and maintainability
- Suggesting focused fixes and refactoring opportunities based on the existing code and requirements
- Identifying useful unit-test scenarios and checking edge cases
- Helping update project documentation, architecture diagrams, and manual verification steps

The tool was used interactively with project-specific prompts and the relevant source code, logs, screenshots, API responses, or requirements as context. Suggestions were reviewed by the developer, implemented only when appropriate, and verified with builds, automated tests, and manual checks. AI was not treated as an autonomous decision-maker, and generated output was not accepted without review.

The following recommended prompts describe the types of assistance used during implementation. Replace the placeholder context with the relevant files, code, logs, or API contract before sending a prompt.

1. **Docker environment review**

   **ภาษาไทย**

   > ช่วยตรวจสอบ Dockerfile และ Docker Compose configuration นี้ว่าใช้สร้าง environment ที่มี database, backend และ frontend ได้ครบตามที่กำหนดหรือไม่ พร้อมบอกจุดที่ควรแก้ไขและวิธีทดสอบ

   **English**

   > Review this Dockerfile and Docker Compose configuration. Verify that it creates the required database, backend, and frontend environment, then identify any issues, recommend specific improvements, and provide verification steps.

2. **Code review**

   **ภาษาไทย**

   > ช่วยรีวิวโค้ดส่วนที่เปลี่ยนแปลงนี้ ตรวจหาจุดที่ควรปรับปรุงด้านความถูกต้อง ความปลอดภัย ความอ่านง่าย การจัดการ error และการดูแลต่อ พร้อมเสนอ patch ที่เหมาะสม

   **English**

   > Review the following code changes for correctness, security, readability, error handling, maintainability, and consistency with the existing project. Explain each finding, prioritize the risks, and propose a focused patch where appropriate.

3. **Backend API unit tests**

   **ภาษาไทย**

   > ช่วยระบุรายการ Backend API unit test ที่ควรมีให้ครอบคลุมอย่างน้อยดังนี้:
   >
   > - Unit test สำหรับ validator, business rule, status transition ของการยืม/คืนหนังสือ, sorting และ pagination
   > - การตรวจสอบ logic การ login และ token รวมถึงกรณี 401/403
   > - Book CRUD: สร้าง, แก้ไข, ลบ, ค้นหา, sort, filter, pagination และข้อมูล Shelf/Location
   > - Borrowing workflow: สร้างคำขอยืม, assign, reject, cancel, request return และ accept return
   > - กรณีสำเร็จ, validation error, malformed input, not found, conflict, duplicate ISBN และ resource ไม่พร้อมใช้งาน
   > - ตรวจสอบสิทธิ์ของ End User กับ Librarian/Administrator และยืนยันว่าไม่สามารถเข้าถึงข้อมูลของผู้ใช้อื่นโดยไม่ได้รับอนุญาต
   >
   > แต่ละ test ควรระบุ expected result และ edge cases ที่สำคัญอย่างชัดเจน พร้อมรันซ้ำได้โดยไม่พึ่งพาลำดับของ test อื่น

   **English**

   > Specify the Backend API unit tests that should exist. At minimum cover:
   >
   > - Unit tests for validators, business rules, borrowing/return status transitions, sorting, and pagination
   > - Login and token-validation logic, including 401 and 403 cases
   > - Book CRUD, search, sorting, filtering, pagination, and Shelf/Location fields
   > - Borrowing workflow: create request, assign, reject, cancel, request return, and accept return
   > - Successful requests, validation errors, malformed input, not-found responses, conflicts, duplicate ISBNs, and unavailable resources
   > - End User versus Librarian/Administrator authorization, including protection against accessing another user’s data
   >
   > Each test should document the expected result and important edge cases. Tests must be isolated, repeatable, and independent of execution order.

4. **Authentication token troubleshooting**

   **ภาษาไทย**

   > ช่วยตรวจสอบปัญหา auth token ตั้งแต่การ login, การเก็บ session, การแนบ Bearer token ใน request, token หมดอายุ, การตอบกลับ 401/403 และการ redirect ของ frontend พร้อมแนะนำวิธีแก้และวิธีทดสอบ

   **English**

   > Investigate this authentication-token issue end to end: login, session storage, Bearer-token attachment, token expiry, 401/403 responses, route guards, and frontend redirects. Identify the root cause, recommend the smallest safe fix, and add verification steps or regression tests.

5. **Backend and frontend test cases**

   **ภาษาไทย**

   > ช่วยเขียน test case สำหรับ backend API และ frontend จาก feature ที่มีอยู่ โดยระบุขั้นตอนทดสอบ ข้อมูลที่ใช้ ผลลัพธ์ที่คาดหวัง และกรณีผิดพลาด เพื่อใส่ไว้ใน README

   **English**

   > Create manual test cases for this backend API and frontend feature. For each case, include prerequisites, test data, exact steps, expected results, negative cases, and cleanup notes so the checklist can be added to the README.

6. **Error investigation**

   **ภาษาไทย**

   > Error นี้เกิดจากอะไร ช่วยวิเคราะห์จากข้อความ error, stack trace และโค้ดที่เกี่ยวข้อง พร้อมอธิบายสาเหตุ วิธีแก้ไขที่เหมาะสม และ test ที่ควรเพิ่มเพื่อป้องกันไม่ให้เกิดซ้ำ

   **English**

   > Analyze this error using the error message, stack trace, relevant source code, and runtime context. Explain the root cause, distinguish confirmed facts from assumptions, propose the safest fix, and recommend a regression test to prevent the problem from returning.

## Logs and Health

Check API health:

```powershell
curl.exe http://localhost:8080/health
curl.exe http://localhost:4200/api/v1/health
```

View logs:

```powershell
docker compose logs --tail=100 api
docker compose logs -f db
docker compose logs -f api
docker compose logs -f web
```

`Ctrl+C` stops following logs without stopping the containers.

## Troubleshooting

- Run Docker commands from the repository root.
- If a port is already in use, change `DB_PORT`, `API_PORT`, or `WEB_PORT` in `.env`, then recreate the affected service.
- If SQL Server credentials no longer match the existing volume, reset the local volume with `docker compose down -v`.
- If the API does not become healthy, check `docker compose logs api` and `docker compose logs db`.
