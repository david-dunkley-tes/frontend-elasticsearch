# Student Search POC

React + Vite frontend, .NET 10 C# backend API, and Elasticsearch in Docker.
The backend uses the official `Elastic.Clients.Elasticsearch` .NET SDK.

Current Elastic stack versions:

```text
Elasticsearch Docker image: 9.4.0
Elastic .NET SDK:           9.4.0
```

## Structure

```text
/
  docker-compose.yml
  Dockerfile.backend
  AGENTS.md
  data/students.seed.json
  tests/backend
  tests/frontend
  src/frontend
  src/backend
```

## Agent Workflow

This repo defines a `/feature-done` workflow in `AGENTS.md` and `.codex/commands/feature-done.md`.

When requested, it builds the backend and frontend, runs backend and frontend tests, updates markdown documentation for drift, and creates a git commit. It must not push; pushing remains a manual user decision.

## Ports

```text
Frontend:      http://localhost:5173
Backend API:   http://localhost:5000
Swagger UI:    http://localhost:5000/swagger
Version:       http://localhost:5000/version
Elasticsearch: http://localhost:9200
```

## Run Locally

Start Elasticsearch and the containerized backend:

```bash
docker compose up -d
```

The backend container bind-mounts `./data` to `/data`, so saved searches created through Docker-backed local development are written to the workspace `data/saved-searches.json` file.

Or start only Elasticsearch and run the backend directly:

```bash
docker compose up -d elasticsearch
dotnet run --project src/backend
```

Swagger UI is available in development at `http://localhost:5000/swagger`. Try-it-out requests automatically use the same Kingfisher Academy dev token as the frontend.

Seed the index:

```bash
curl -X POST http://localhost:5000/api/admin/reindex \
  -H "Authorization: Bearer <dev-token>"
```

Start the frontend:

```bash
cd src/frontend
npm install
npm run dev
```

## Search Behaviour

- One Elasticsearch document represents one student.
- School and trust data are denormalized into each student document.
- Free-text search covers student ID, forename, surname, full name, year group, school name, school address, and trust name.
- Non-empty free-text searches sort by Elasticsearch relevance.
- Empty searches sort by student surname, then forename.
- Query strategy combines exact student ID matching, phrase boosts, and fuzzy `multi_match` with `type: best_fields` and `fuzziness: AUTO`.
- Facets are returned from `POST /api/search`.
- Facets use self-excluding counts.
- Facet groups with only one available option are hidden in the frontend.
- The year group facet is displayed in numeric year order, while preserving labels such as `Year 8`.
- Trust can be `null`; the UI displays this as `No trust`.
- Result cards show whether a text search matched student, school, or trust fields.
- Search query, selected filters, and page can be shared through the URL query string.
- School and trust names in results can be used to drill down into filtered searches.
- Searches can be saved, reapplied, and deleted from the frontend.
- Saved searches are scoped to the authenticated dev token subject and stored in `data/saved-searches.json` by default.
- The frontend reads `/version` on startup and includes the API version in the browser page title.
- Debug mode is off by default; the Reindex and Debug controls are in the page footer.

## Deep Links

Search state is encoded in the frontend URL. `q` stores the free-text query, `page` stores the current page when it is greater than one, and each selected filter is stored under its facet id. Multi-select filters use repeated parameters.

```text
http://127.0.0.1:5173/?q=westbrook&school=westbrook+college&yearGroup=year+9
```

## Development Authorization

Local development uses an unsigned dev bearer token with a base64url-encoded JSON payload:

```json
{
  "sub": "dev-global-admin",
  "name": "Global Admin",
  "scopes": [{ "type": "global" }]
}
```

The frontend sends this as `Authorization: Bearer <token>`. The backend middleware decodes it into a `ClaimsPrincipal`, and search authorization resolves the token scopes into an allowed school set. Users only see, filter, facet, and drill into the data allowed by their scopes; unauthorized data is not returned in results or facet counts.

Supported development scope types are `global`, `trust`, `school`, and `schoolGroup`. Scopes are additive, so a token can combine a trust with schools outside that trust. The current frontend dev token is scoped to `Kingfisher Academy` using `SCH-KINGFISHER`.

All `/api` endpoints require the bearer token. Standard health checks are unauthenticated at `/health/live` and `/health/ready`.

Build/version metadata is unauthenticated at `/version`. It reports the service name, version, commit, build time, and environment. `APP_VERSION`, `GIT_COMMIT`, and `BUILD_TIME` can be injected by Docker or CI; local development falls back to assembly version, `local`, and `unknown`.

## Useful Test Searches

```text
Harrington
Harington
hrington
S10001
Northshire
Riverside
Year 8
Station Road
Southbank
City Learning
Cedar Grove
Harbour View
Beacon Hill
No trust
```

## Backend Structure Rule

`Program.cs` should stay composition-only: service registration, middleware, controller mapping, and app startup. Controllers, models, services, and infrastructure should live in focused files under `src/backend`.

The service layer must stay decoupled from Elasticsearch. Application services depend on index abstractions such as `IStudentSearchIndex` and `IStudentIndexSeeder`; Elasticsearch SDK usage, query JSON, mappings, and bulk/index operations belong under `Infrastructure/Elasticsearch`.

## API

### `POST /api/search`

```json
{
  "query": "harrington",
  "filters": {
    "yearGroup": ["year 8"],
    "trust": ["northshire learning trust"]
  },
  "sort": "relevance",
  "page": 1,
  "pageSize": 10,
  "debugMode": true
}
```

### `POST /api/admin/reindex`

Development-only endpoint. Deletes and recreates the configured Elasticsearch index, then bulk indexes `data/students.seed.json`.

### `GET /api/auth/me`

Returns the current dev token subject, display name, and scopes.

### `GET /api/saved-searches`

Returns saved searches ordered by creation time, newest first.

### `POST /api/saved-searches`

Saves the current query, filters, sort, and page size.

```json
{
  "name": "Year 8 Harrington",
  "query": "harrington",
  "filters": {
    "yearGroup": ["year 8"]
  },
  "sort": "relevance",
  "pageSize": 10
}
```

### `DELETE /api/saved-searches/{id}`

Deletes a saved search.

### `GET /health/live`

Returns process liveness only. This endpoint does not check Elasticsearch or other external dependencies.

### `GET /health/ready`

Returns readiness as JSON. This endpoint checks Elasticsearch and returns unhealthy when Elasticsearch cannot serve the cluster health request.

### `GET /version`

Returns service build/version metadata.
