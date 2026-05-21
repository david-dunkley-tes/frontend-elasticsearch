# `/feature-done`

Run the feature completion workflow for this repository.

## Steps

1. Confirm all projects build successfully:
   - `dotnet build src/backend/StudentSearch.Api.csproj`
   - `npm run build` from `src/frontend`
2. Run all tests and confirm they pass:
   - `dotnet test tests/backend/StudentSearch.Api.Tests.csproj`
   - `npm test` from `src/frontend`
3. Review markdown documentation for drift and update relevant files, including `README.md` and `AGENTS.md`, when behavior, setup, commands, structure, or conventions changed.
4. Create a git commit containing the completed feature and documentation updates.

Never push the commit. Pushing is always the user's decision.
