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
Elasticsearch: http://localhost:9200
```

## Run Locally

Start Elasticsearch:

```bash
docker compose up -d
```

Start the backend:

```bash
dotnet run --project src/backend
```

Seed the index:

```bash
curl -X POST http://localhost:5000/api/admin/reindex
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
- Trust can be `null`; the UI displays this as `No trust`.
- Result cards show whether a text search matched student, school, or trust fields.
- Search query, selected filters, and page can be shared through the URL query string.
- Searches can be saved, reapplied, and deleted from the frontend.
- Saved searches are stored in `data/saved-searches.json` by default.

## Deep Links

Search state is encoded in the frontend URL. `q` stores the free-text query, `page` stores the current page when it is greater than one, and each selected filter is stored under its facet id. Multi-select filters use repeated parameters.

```text
http://127.0.0.1:5173/?q=westbrook&school=westbrook+college&yearGroup=year+9
```

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

### `GET /api/health`

Returns a simple health status.
