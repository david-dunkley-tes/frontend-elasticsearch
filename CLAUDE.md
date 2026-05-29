# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Stack & Layout

Three pieces that always run together for local dev:

- `src/backend` — .NET 10 ASP.NET Core Web API (`StudentSearch.Api`), port `5000`. Uses the official `Elastic.Clients.Elasticsearch` SDK (9.4.0).
- `src/frontend` — React 19 + Vite + TypeScript SPA, port `5173`.
- `docker-compose.yml` — single-node Elasticsearch 9.4.0 on port `9200` with security disabled, plus a `backend` service built from `Dockerfile.backend` (port `5000`, gated on a healthy Elasticsearch). Locally the backend reads `Elasticsearch:Url` / `Elasticsearch:IndexName` from `src/backend/appsettings.json`; the container overrides the URL via the `Elasticsearch__Url` env var.

Tests live in `tests/backend` and `tests/frontend` (separate projects, not co-located with sources).

## Common Commands

Backend (run from repo root):

```bash
docker compose up -d                                       # start the full stack (Elasticsearch + backend on :5000)
docker compose up -d elasticsearch                         # start only Elasticsearch, then run the API locally:
dotnet run --project src/backend                           # start API on :5000
dotnet build src/backend/StudentSearch.Api.csproj
dotnet test  tests/backend/StudentSearch.Api.Tests.csproj
dotnet test  tests/backend/StudentSearch.Api.Tests.csproj --filter "FullyQualifiedName~SearchControllerTests"
```

`docker compose up -d` runs the backend in a container on :5000, so don't also `dotnet run` the API at the same time — start only `elasticsearch` with compose when you want to run the backend locally. The backend container bind-mounts `./data` to `/data`, so containerized saved-search writes land in the workspace data folder.

Swagger UI is available in development at `http://localhost:5000/swagger`. Try-it-out requests automatically use the same Kingfisher Academy dev token as the frontend.

Build/version metadata is available without auth at `/version`. It reads `APP_VERSION`, `GIT_COMMIT`, and `BUILD_TIME` when supplied by Docker or CI, otherwise it falls back to assembly version, `local`, and `unknown`.

Frontend (run from `src/frontend`):

```bash
npm install
npm run dev      # vite dev server on 127.0.0.1:5173
npm run build    # tsc -b && vite build
npm test         # vitest run, config at tests/frontend/vitest.config.ts
npx vitest run tests/frontend/App.test.tsx   # single file (run from src/frontend)
```

Seeding Elasticsearch (dev token required — see Authorization below):

```bash
curl -X POST http://localhost:5000/api/admin/reindex -H "Authorization: Bearer <dev-token>"
```

The `/feature-done` workflow (defined in `AGENTS.md` and `.codex/commands/feature-done.md`) runs both builds, both test suites, updates drifted markdown, and creates a commit — **never pushes**. Pushing is always the user's call.

## Backend Architecture

`Program.cs` is **composition-only**: service registration (including Swagger and health checks), CORS, middleware, and route mapping — controllers, health-check endpoints (`/health/live`, `/health/ready`), and Swagger UI. Do not add request-handling logic there; the one accepted exception is trivial metadata endpoints with no business logic, like `/version`, which is mapped inline and just delegates to `IVersionInfoProvider`. Everything else lives under:

- `Controllers/` — thin HTTP entry points (`SearchController`, `AdminController`, `AuthController`, `SavedSearchesController`). Resolve the caller's scope via `IAuthorizationScopeResolver` and delegate to a service.
- `Interfaces/` (namespace `StudentSearch.Api.Interfaces`) — every public service/seam contract lives here (`IStudentSearchService`, `IStudentSearchIndex`, `IStudentIndexSeeder`, `ISafeguardingService`, `IEmbeddingClient`, `IAnthropicClient`, `IStudentKnnRetriever`, etc.). Contract-adjacent records (e.g. `KnnHit`/`KnnSearchResult`, `AnthropicMessageRequest`/`AnthropicMessageResponse`) live alongside their interface.
- `Services/` — application logic, implementations only. **Must stay decoupled from Elasticsearch.** `StudentSearchService` normalizes a request then calls `IStudentSearchIndex`; `ReindexService` calls `IStudentIndexSeeder`. The interfaces in `Interfaces/` are the seam — never reference `Elastic.Clients.Elasticsearch` or raw ES JSON from `Services/`. When adding a new service, put the interface under `Interfaces/` and the implementation under `Services/`.
- `Infrastructure/Elasticsearch/` — the only place ES SDK usage, query JSON, mappings, and bulk operations are allowed. `ElasticsearchStudentSearchIndex` builds the search payload; `ElasticsearchStudentIndexSeeder` recreates the index from `data/students.seed.json`; `ElasticsearchGateway` is the low-level HTTP wrapper.
- `Models/` — request/response DTOs and domain records (one ES document = one student, with school, trust, and `classGroup` (class name + teacher) denormalized inline). Within a school year there are two classes named after trees (e.g. Acorn/Pine), each with one teacher. The teacher name is PII: `NarrativeRedactor` scrubs it to `[teacher]` (alongside `[student]`/`[school]`) before any narrative is embedded or sent to Voyage/Anthropic.
- `Configuration/SearchConfiguration` — resolves ES URL, index name, and absolute paths to `data/students.seed.json` and `data/saved-searches.json` (paths are relative to `ContentRootPath/../..`).

### Authorization model

All `/api/*` routes require a bearer token. Anything outside `/api` is unauthenticated — the health checks at `/health/live` and `/health/ready`, and the build metadata at `/version`. The token is a base64url-encoded JSON payload (no signature — dev only):

```json
{ "sub": "...", "name": "...", "scopes": [{ "type": "global" | "school" | "trust", "role": ["DSL"], ... }] }
```

Scope types are `global`, `trust`, `school` (there is no `schoolGroup` — a multi-school user is just several `school` scopes). Each scope may carry a `role` list; roles are looked up by name (see `Models/Roles.cs`).

Flow:
1. `DevBearerAuthenticationMiddleware` decodes the token, validates scope shapes, and writes the scopes JSON into a `"scopes"` claim on `ClaimsPrincipal`.
2. `AuthorizationScopeResolver.ResolveAsync(User)` produces the **viewing** scope — `Global` or a concrete `SchoolIds` set (roles ignored; `trust` expanded via `students.seed.json`; scopes additive).
3. `AuthorizationScopeResolver.ResolveRoleScopeAsync(User, role)` produces the scope for a given **role**: for each school the most specific covering scope wins (`school` > `trust` > `global`) and the school is included when that scope's `role` list contains the role. This is generic — safeguarding is just the first consumer, calling it with `Roles.Dsl`.
4. `ElasticsearchStudentSearchIndex` applies the **viewing** scope as a filter on the main query and every facet aggregation, and separately **nulls `safeguardingLog`** for any student whose school is outside the caller's **DSL** scope — so a user can view trust-wide pupils yet see safeguarding only for the schools they are DSL for. `SafeguardingController` gates `availability`/`Ask` on the DSL scope (`AuthorizedSchoolScope.GrantsAnySchool`) and restricts RAG retrieval to it.

When adding endpoints that touch student data, always resolve the viewing scope (and, for safeguarding, the DSL role scope) and pass it through to the index layer — do not assume the user can see everything.

### Search behaviour invariants

These are deliberate and called out in `README.md`; preserve them when editing search code:

- Non-empty `query` sorts by Elasticsearch `_score`; empty `query` sorts by `student.surname.keyword` then `student.foreName.keyword`.
- Free-text query combines exact ID match, phrase boosts, and a fuzzy `multi_match` (`type: best_fields`, `fuzziness: AUTO`) across student/school/trust/class fields (including `classGroup.name` and `classGroup.teacher`).
- Facets use **self-excluding counts** (each facet aggregation excludes its own selected filter).
- The frontend hides facet groups with only one available option and sorts the `yearGroup` facet by the numeric year while preserving labels.
- The `classTeacher` facet (combined "Class - Teacher", backed by the case-preserving `classGroup.label` keyword) is only rendered once the result set is narrow — the frontend hides it until there are ≤5 options. Its labels are shown verbatim (not title-cased), via the `RawLabel` flag on `FacetDefinition`.
- `trust` can be `null`; the index uses sentinel `__NO_TRUST__` for the "missing" bucket and the UI renders it as "No trust".
- Page size is clamped to `[1, 100]` in `StudentSearchService.NormalizeRequest`.

### Saved searches

Persisted to `data/saved-searches.json` (gitignored) and scoped by the authenticated token subject (`sub`). The frontend never sends an owner id; `SavedSearchesController` derives it from the current user. `SavedSearchService` is a `Singleton` and serializes file writes itself — don't introduce a second writer.

## Frontend Architecture

`src/frontend/src/App.tsx` is the single stateful component: it owns query/filters/page/debugMode, syncs them to/from the URL query string (`q`, `page`, and one param per facet — multi-select uses repeated params), and reacts to `popstate`. Debug mode is off by default and is toggled from the page footer alongside Reindex. Effects re-fetch search results whenever the request payload changes (with `AbortController` to cancel stale requests). Components under `components/` are presentational.

API calls go through `src/api/studentSearchApi.ts`, which hard-codes `API_BASE = http://<hostname>:5000` and builds a dev bearer token in-browser via `encodeDevAccessToken`. The current dev token is scoped to `SCH-KINGFISHER` (Kingfisher Academy); change the payload there to test other scope shapes.

Vitest config (`tests/frontend/vitest.config.ts`) aliases React/testing-library/lucide directly to files in `src/frontend/node_modules` — tests run from the **repo root** but resolve dependencies from the frontend workspace. If you add a new dep that tests import, you may need to add an alias.

## Conventions

- **C# style:** prefer primary constructors for classes with injected dependencies or simple test stubs. The existing services and controllers all follow this pattern (`public sealed class Foo(IDep dep) : IFoo`).
- Markdown docs (`README.md`, `AGENTS.md`, this file) are part of the source — update them when behaviour, setup, commands, structure, or conventions change.
