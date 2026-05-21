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
  data/students.seed.json
  src/frontend
  src/backend
```

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

### `GET /api/health`

Returns a simple health status.
