# Frontend

Angular 21 application. The current screen displays API connectivity; library management screens are not implemented yet.

## Development Server

Use Node.js 22.12 or newer within the Node 22 release line.

From the repository root, start the backend and free port `4200`:

```powershell
docker compose stop web
docker compose up --build -d db api
cd frontend
npm ci
npm start
```

Open http://localhost:4200. The development server reloads the application when source files change.

`proxy.conf.json` forwards `/api/**` to `http://127.0.0.1:8080`. If you change `API_PORT` in the root `.env`, update the proxy target and restart the development server.

Compose reads the root `.env`; Angular does not. Do not put database passwords or JWT signing keys in frontend configuration.

Press Ctrl+C to stop the development server. To return to the Nginx container, run `docker compose up --build -d web` from the repository root.

## Build

From `frontend/`:

```powershell
npm run build
```

Output goes to `dist/library-management-web/browser`. The frontend Docker image serves this directory through Nginx and proxies `/api/` to the API.

## Tests

```powershell
npm test -- --watch=false
```

Tests use Angular's Vitest runner with jsdom and mocked HTTP responses. They cover the checking, online, and offline states and do not require Docker.

Use `npm test` for interactive watch mode. No end-to-end test runner is configured.
