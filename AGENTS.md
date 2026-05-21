# Agent Instructions

## Slash Commands

### `/feature-done`

When the user enters `/feature-done`, complete the feature handoff workflow:

1. Confirm all projects build successfully:
   - Backend: `dotnet build src/backend/StudentSearch.Api.csproj`
   - Frontend: `npm run build` from `src/frontend`
2. Run all tests and confirm they pass:
   - Backend: `dotnet test tests/backend/StudentSearch.Api.Tests.csproj`
   - Frontend: `npm test` from `src/frontend`
3. Review markdown documentation for drift and update relevant files, including `README.md` and this `AGENTS.md`, when behavior, setup, commands, structure, or conventions changed.
4. Create a git commit containing the completed feature and documentation updates.

Do not push the commit. Pushing is always the user's decision.
