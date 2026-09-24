# Library Management System

Technical assessment project for a Library Management System using:

- .NET 8 / ASP.NET Core Web API
- Angular
- Microsoft SQL Server
- Docker Compose

## Repository Structure

```text
LibraryManagementSystem/
├── backend/
│   ├── src/
│   └── tests/
├── frontend/
│   └── src/
├── assets/
├── scripts/
├── .env.example
├── .gitignore
├── docker-compose.yml
└── README.md
```

## Current Status

Phase 1 foundation is prepared:

- Root project folders
- Environment variable template
- Git ignore rules
- README skeleton

Application code, Dockerfiles, and Docker Compose will be added in later phases.

## Planned Local Setup

After infrastructure is implemented, the expected startup flow will be:

```bash
copy .env.example .env
docker compose up --build
```

Expected local URLs:

```text
Frontend: http://localhost:4200
API:      http://localhost:8080
Swagger:  http://localhost:8080/swagger
```
