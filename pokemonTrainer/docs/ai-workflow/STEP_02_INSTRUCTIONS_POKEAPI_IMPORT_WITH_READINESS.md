# Step 02 Instructions — PokeAPI Import with Background Readiness

## Purpose

Implement the Pokémon data import process from PokeAPI into the local SQL Server database.

The system must know all Pokémon currently available in PokeAPI. The import process should run in the background when the API starts, while exposing a readiness/status mechanism so API consumers do not receive partial Pokémon data during the import.

The frontend must never call PokeAPI directly. All external data access is performed by the ASP.NET Core Web API backend.

---

## Design Decision

Use a background import with explicit readiness status.

The API should not block server startup for a potentially long import process. However, endpoints that depend on Pokémon data must not return partial results while the import is still running.

The chosen design:

```text
Server starts
↓
Background worker starts Pokémon import
↓
Import status becomes Running
↓
Pokémon endpoints check readiness before returning data
↓
If import is still running:
    return 503 / loading response
↓
When import completes:
    status becomes Completed
↓
Pokémon endpoints return normal data
```

This avoids partial data responses while keeping server startup responsive.

---

## Import Strategy

The import process must be idempotent.

It should not re-import all Pokémon every time.

Instead:

1. Fetch the lightweight Pokémon resource list from PokeAPI.
2. Extract remote Pokémon IDs from the resource URLs.
3. Load local `PokeApiId` values from SQL Server.
4. Compare remote IDs with local IDs.
5. Fetch details only for Pokémon missing locally.
6. Insert only missing Pokémon into the local database.

Behavior:

```text
First run:
Local DB is empty
→ all remote Pokémon are missing
→ import all Pokémon

Later runs:
Local DB already has Pokémon
→ compare remote IDs with local IDs
→ import only newly missing Pokémon
```

---

## Target Result

At the end of this step, the backend should include:

- PokeAPI DTOs
- Typed `PokeApiClient`
- `PokemonImportResult`
- `PokemonImportStatusResponse`
- `PokemonImportStatusService`
- `PokemonImportService`
- `PokemonStartupImportWorker`
- `PokemonImportStatusController`
- Background import on API startup
- Readiness status endpoint
- Import of only missing Pokémon
- No duplicate Pokémon records
- Imported data saved into:
  - `Pokemons`
  - `PokemonTypes`
  - `PokemonPokemonTypes`

---

## Expected Project Structure Additions

```text
pokemonTrainer/
│
├── Controllers/
│   └── PokemonImportStatusController.cs
│
├── DTOs/
│   ├── PokeApi/
│   │   ├── PokeApiListResponse.cs
│   │   ├── PokeApiNamedResource.cs
│   │   ├── PokeApiPokemonAbilitySlot.cs
│   │   ├── PokeApiPokemonDetails.cs
│   │   ├── PokeApiPokemonSprites.cs
│   │   ├── PokeApiPokemonStatSlot.cs
│   │   └── PokeApiPokemonTypeSlot.cs
│   │
│   └── PokemonImport/
│       ├── PokemonImportResult.cs
│       └── PokemonImportStatusResponse.cs
│
├── Infrastructure/
│   └── PokeApiClient.cs
│
├── Services/
│   ├── PokemonImportService.cs
│   └── PokemonImportStatusService.cs
│
└── Workers/
    └── PokemonStartupImportWorker.cs
```

---

# Phase 1 — Add Import Status Foundation

## Goal

Create a small in-memory status service that tracks the Pokémon import state.

The system should be able to represent:

- NotStarted
- Running
- Completed
- Failed

The status service will later be used by Pokémon endpoints to avoid returning partial data.

## Files

Create:

```text
DTOs/PokemonImport/PokemonImportResult.cs
DTOs/PokemonImport/PokemonImportStatusResponse.cs
Services/PokemonImportStatusService.cs
Controllers/PokemonImportStatusController.cs
```

## Registration

Register the status service as singleton:

```csharp
builder.Services.AddSingleton<PokemonImportStatusService>();
```

## Validation

Run:

```text
GET /api/pokemon-import-status
```

Expected result before import starts:

```json
{
  "status": "NotStarted",
  "isReady": false,
  "lastMessage": "Pokémon import has not started yet."
}
```

Stop and wait for approval.

---

# Phase 2 — Add PokeAPI DTOs

Create DTOs that represent only the fields required by the application.

Required fields:

- ID
- Name
- Height
- Weight
- Base experience
- Types
- Stats
- Abilities
- Image URL

Do not model the entire PokeAPI response.

Image handling strategy:

- Prefer `sprites.other.official-artwork.front_default`.
- If unavailable, fall back to `sprites.front_default`.
- Do not download images into the backend at this stage.
- The frontend should later handle broken image URLs with a local placeholder image.
- In a production-grade implementation, image caching or Blob Storage/CDN could be added later.

Stop and wait for approval.

---

# Phase 3 — Add PokeApiClient

Create a typed HTTP client under `Infrastructure`.

The client should expose:

```csharp
Task<PokeApiListResponse?> GetPokemonListAsync(
    int limit,
    int offset = 0,
    CancellationToken cancellationToken = default)

Task<PokeApiPokemonDetails?> GetPokemonDetailsAsync(
    string nameOrId,
    CancellationToken cancellationToken = default)
```

Register it with:

```csharp
builder.Services.AddHttpClient<PokeApiClient>(client =>
{
    client.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
    client.Timeout = TimeSpan.FromSeconds(20);
});
```

Stop and wait for approval.

---

# Phase 4 — Add PokemonImportService

Create `PokemonImportService`.

The service should:

1. Fetch all remote Pokémon references from PokeAPI in pages.
2. Extract Pokémon IDs from the PokeAPI URLs.
3. Load local `PokeApiId` values from SQL Server.
4. Identify missing Pokémon.
5. Fetch details only for missing Pokémon.
6. Create missing types if needed.
7. Save Pokémon and type relations.
8. Store stats and abilities as JSON.
9. Use `PokeApiId` to prevent duplicates.
10. Return `PokemonImportResult`.

The method should support:

```csharp
Task<PokemonImportResult> ImportMissingAsync(
    int? maxCount = null,
    CancellationToken cancellationToken = default)
```

`maxCount` is only for development validation. The final startup import should use `null`.

Stop and wait for approval.

---

# Phase 5 — Add Startup Background Worker

Create `PokemonStartupImportWorker`.

The worker should:

1. Mark import status as `Running`.
2. Resolve `PokemonImportService` from a scoped service provider.
3. Run `ImportMissingAsync(maxCount: null)`.
4. Mark status as `Completed` if the import succeeds.
5. Mark status as `Failed` if the import fails.

Because hosted services are effectively singleton services, do not inject `ApplicationDbContext` or `PokemonImportService` directly into the worker.

Use `IServiceScopeFactory`.

Stop and wait for approval.

---

# Phase 6 — Protect Pokémon Data Endpoints

When `PokemonController` is created later, every endpoint that depends on Pokémon data should check readiness first.

Example behavior:

```text
If import status is not ready:
    return 503 Service Unavailable
Else:
    return Pokémon data
```

Example response:

```json
{
  "status": "ImportInProgress",
  "message": "Pokémon data is still loading. Please try again shortly."
}
```

This prevents the client from receiving partial data during startup import.

---

# Program.cs Required Registrations

At the end of this step, `Program.cs` should include:

```csharp
builder.Services.AddHttpClient<PokeApiClient>(client =>
{
    client.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddSingleton<PokemonImportStatusService>();
builder.Services.AddScoped<PokemonImportService>();
builder.Services.AddHostedService<PokemonStartupImportWorker>();
```

---

# Validation

## First Run

Expected behavior:

```text
Status: Running
Import starts in the background
Missing Pokémon are inserted into SQL Server
Status becomes Completed
```

## Later Runs

Expected behavior:

```text
Remote IDs are loaded
Local IDs are loaded
Missing count is usually 0
No Pokémon details are fetched for existing Pokémon
No duplicates are inserted
Status becomes Completed
```

## Database Checks

```sql
SELECT COUNT(*) FROM Pokemons;
SELECT COUNT(*) FROM PokemonTypes;
SELECT COUNT(*) FROM PokemonPokemonTypes;
```

---

# Notes

The import process detects missing Pokémon by comparing remote PokeAPI IDs with local `PokeApiId` values.

This detects newly added Pokémon, but does not refresh metadata for existing Pokémon.

A future enhancement could add a separate metadata refresh process if needed.

---

# Final Step Summary Format

At the end of this step, provide:

```text
Step summary:
- Goal:
- Files created:
- Files modified:
- Database changes:
- How to test:
- Notes:
- Next recommended step:

Waiting for approval before continuing.
```
