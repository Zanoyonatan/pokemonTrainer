# STEP 07 — Database Unavailable Handling and Pokémon Catalog Read-Only Cache

## Purpose

Improve backend resilience for the required edge case where the database is unavailable.

This step has two goals:

1. Return a clear `503 Service Unavailable` response when SQL Server / LocalDB is unavailable.
2. Add a lightweight read-only Pokémon catalog cache so selected catalog features can continue working if the database becomes unavailable after the cache was already warmed.

This step should not change unrelated business logic.

---

## Important Architecture Rules

- SQL Server remains the source of truth.
- The cache is read-only fallback only.
- Do not write user data to cache.
- Do not sync user changes from cache back to DB.
- Do not allow Dream Team writes while DB is unavailable.
- Do not allow Register/Login while DB is unavailable.
- Do not call PokeAPI as a fallback for user requests.
- PokeAPI is still used only by the server import process.
- Controllers must remain thin.
- Business logic must remain in services.
- If the cache is empty and the DB is unavailable, return `503 DATABASE_UNAVAILABLE`.

---

## Expected Behavior

### Normal Mode

When the DB is available:

```text
All endpoints work normally against SQL Server.
The Pokémon catalog cache is refreshed from SQL Server.
SQL Server is the source of truth.
```

### DB Unavailable Mode

When the DB is unavailable:

```text
Register/Login -> 503 DATABASE_UNAVAILABLE
Dream Team read/write -> 503 DATABASE_UNAVAILABLE
Dream Team Analyzer -> 503 DATABASE_UNAVAILABLE

Pokémon catalog read endpoints -> use cache if cache exists
Smart Search -> use cache if cache exists
Nickname Generator -> use cache if cache exists
```

### DB Comes Back

When the DB becomes available again:

```text
The system automatically returns to SQL Server.
The Pokémon catalog cache is refreshed from SQL Server.
No merge is required because no writes were accepted into cache.
```

---

# User Experience When DB Is Unavailable

When the database is unavailable, the system should enter a partial read-only mode.

This is important because the system should not behave as if everything is working normally, but it also should not fully block the user if cached Pokémon catalog data is available.

---

## If the User Is Not Logged In

Login and Register require the database.

The API should return:

```http
503 Service Unavailable
```

With:

```json
{
  "errorCode": "DATABASE_UNAVAILABLE",
  "message": "The database is currently unavailable."
}
```

The future client should show a friendly message such as:

```text
The system is temporarily unavailable. Please try again in a few minutes.
```

Reason:

```text
Without the database, the system cannot validate credentials, create users, or load user identity data.
```

---

## If the User Is Already Logged In and Has a Valid JWT

JWT validation may still work because the token can be validated without querying the DB.

If the Pokémon catalog cache is available, the user may continue using read-only Pokémon features:

- Browse Pokémon catalog
- View Pokémon details
- Use Smart Search
- Generate nickname suggestions

The future client should show a banner such as:

```text
Temporary read-only mode: the database is currently unavailable. You can still browse cached Pokémon data, but personal actions are disabled.
```

The API may optionally include an indicator in read-only responses if it does not break existing contracts, for example:

```json
{
  "source": "cache",
  "isReadOnlyFallback": true
}
```

This is optional. Do not break existing DTOs unless needed.

---

## Features That Must Be Disabled While DB Is Unavailable

The following features must not work without DB:

- Register
- Login
- Dream Team read
- Dream Team create
- Dream Team update
- Dream Team delete
- Dream Team Analyzer

Reason:

```text
These features rely on user-specific or transactional data.
SQL Server is the source of truth.
Using cache for these features would create consistency and security risks.
```

Expected response:

```http
503 Service Unavailable
```

```json
{
  "errorCode": "DATABASE_UNAVAILABLE",
  "message": "The database is currently unavailable."
}
```

---

## If Cache Is Not Available

If DB is unavailable and the Pokémon catalog cache was not warmed, even read-only catalog features should return:

```http
503 Service Unavailable
```

With:

```json
{
  "errorCode": "DATABASE_UNAVAILABLE",
  "message": "The database is currently unavailable and no cached Pokémon catalog is available."
}
```

The future client should show:

```text
The Pokémon catalog is temporarily unavailable. Please try again later.
```

---

## When DB Becomes Available Again

The system should automatically return to normal DB mode.

Expected behavior:

```text
DB restored
↓
API reads from SQL Server again
↓
Pokémon catalog cache refreshes from SQL Server
↓
Personal features become available again
```

No merge is required because no writes were accepted into cache while DB was unavailable.

---

# Recommended Scope

Implement in two parts, but it is acceptable to implement both in one controlled pass if the changes remain minimal and focused.

---

# Part 1 — Explicit DB Unavailable Handling

## Goal

Update global error handling so database connection failures return:

```http
503 Service Unavailable
```

Response:

```json
{
  "errorCode": "DATABASE_UNAVAILABLE",
  "message": "The database is currently unavailable.",
  "traceId": "..."
}
```

Instead of a generic 500 error.

---

## Files to Modify

Expected file:

```text
Middleware/GlobalExceptionHandlingMiddleware.cs
```

Possibly:

```text
Controllers/HealthController.cs
```

Do not modify unrelated files.

---

## Implementation Notes

Inside `GlobalExceptionHandlingMiddleware`, add a helper method:

```csharp
private static bool IsDatabaseUnavailableException(Exception exception)
```

It should inspect the exception and inner exceptions.

Treat the following as database unavailable:

- `Microsoft.Data.SqlClient.SqlException`
- `System.Data.Common.DbException`
- timeout exceptions caused by DB access
- known EF Core database connection failures

Suggested logic:

```text
Walk through exception and all inner exceptions.
If any inner exception is SqlException or DbException, return true.
If timeout / connection message clearly indicates SQL connection failure, return true.
Otherwise return false.
```

Then set:

```text
StatusCode = 503
ErrorCode = DATABASE_UNAVAILABLE
Message = The database is currently unavailable.
```

For non-DB exceptions, keep existing behavior:

```text
StatusCode = 500
ErrorCode = INTERNAL_SERVER_ERROR
Message = An unexpected error occurred.
```

In Development only, it is acceptable to include `details`.

---

## Optional DB Health Endpoint

Add or update:

```http
GET /api/health/db
```

Expected behavior:

If DB is available:

```http
200 OK
```

```json
{
  "status": "Healthy",
  "database": "Available"
}
```

If DB is unavailable:

```http
503 Service Unavailable
```

```json
{
  "status": "Unhealthy",
  "database": "Unavailable"
}
```

Implementation:

- Inject `ApplicationDbContext`.
- Use `await dbContext.Database.CanConnectAsync(cancellationToken)`.
- Catch DB exceptions and return 503.

Keep existing `/api/health` unchanged if possible.

---

# Part 2 — Pokémon Catalog Read-Only Cache

## Goal

Add an in-memory cache for Pokémon catalog data.

The cache should allow read-only catalog features to work if the database becomes unavailable after the cache was already loaded.

---

## Recommended Cache Type

Use `IMemoryCache`.

Register in `Program.cs`:

```csharp
builder.Services.AddMemoryCache();
```

Do not add Redis or external infrastructure for this task.

---

## New Service

Create:

```text
Services/PokemonCatalogCacheService.cs
```

Responsibilities:

- Load Pokémon catalog from SQL Server when DB is available.
- Store a read-only snapshot in memory.
- Return cached data if DB is unavailable.
- Expose whether the returned data came from cache.
- Never write user data.
- Never call PokeAPI.

---

## Recommended Internal Model

Create an internal DTO/model for cache items if needed:

```text
DTOs/Pokemon/PokemonCatalogCacheItem.cs
```

or keep it inside the service if preferred.

Suggested fields:

```csharp
public class PokemonCatalogCacheItem
{
    public int Id { get; set; }

    public int PokeApiId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int Height { get; set; }

    public int Weight { get; set; }

    public int? BaseExperience { get; set; }

    public int Hp { get; set; }

    public int Attack { get; set; }

    public int Defense { get; set; }

    public int SpecialAttack { get; set; }

    public int SpecialDefense { get; set; }

    public int Speed { get; set; }

    public List<string> Types { get; set; } = new();
}
```

The cache must include all fields needed by:

- Pokémon list
- Pokémon details
- Smart Search
- Nickname Generator

---

## Cache Loading Strategy

Preferred behavior:

```text
When a catalog read request arrives:
    Try reading from SQL Server.
    If successful:
        Map DB entities to cache items.
        Refresh IMemoryCache.
        Return DB result.

    If DB fails:
        Try reading from IMemoryCache.
        If cache exists:
            Return cached result.
        If cache does not exist:
            Return 503 DATABASE_UNAVAILABLE.
```

This keeps SQL Server as source of truth and cache as fallback.

---

## Alternative Warmup Strategy

Optionally warm the cache after Pokémon import succeeds.

Possible locations:

```text
PokemonStartupImportWorker
PokemonImportService
A new hosted service
```

Keep this optional. Do not overcomplicate.

Minimum acceptable implementation:

```text
Cache is refreshed when catalog endpoints successfully read from DB.
```

---

# Part 3 — Use Cache in Pokémon Catalog Endpoints

## Goal

Make Pokémon read endpoints resilient.

Expected endpoints:

```http
GET /api/pokemon
GET /api/pokemon/{id}
```

When DB is available:

```text
Return data from SQL Server.
Refresh cache.
```

When DB is unavailable and cache exists:

```text
Return data from cache.
Optionally include a flag in response if existing DTO supports it.
Do not break existing API contracts if not needed.
```

When DB is unavailable and cache does not exist:

```http
503 Service Unavailable
```

```json
{
  "errorCode": "DATABASE_UNAVAILABLE",
  "message": "The database is currently unavailable and no cached Pokémon catalog is available."
}
```

---

## Important

Do not remove authorization.

If the user already has a valid JWT token, read-only catalog endpoints may still work because JWT validation does not require DB access.

However:

```text
Login and Register still require DB.
```

This is acceptable.

---

# Part 4 — Use Cache in Smart Search

## Goal

Allow Smart Search to work from cache when DB is unavailable and cache is available.

Expected endpoint:

```http
POST /api/pokemon-smart-search
```

Behavior:

```text
Try normal DB-based search.
If DB is available:
    return DB result and refresh cache if needed.

If DB is unavailable:
    run equivalent filtering/sorting over cached catalog items.
```

Supported cached search behavior should match existing criteria as much as possible:

- Type filter
- Name search
- Height / Weight filters
- Sort by:
  - hp
  - attack
  - defense
  - specialAttack
  - specialDefense
  - speed
  - height
  - weight
  - baseExperience
  - totalStats
- requestedCount
- pagination

Do not call Gemini differently.

Gemini / rule-based parsing should still create criteria first.

Then the result provider may be DB or cache.

---

# Part 5 — Use Cache in Nickname Generator

## Goal

Allow nickname generation for Pokémon if DB is unavailable and cache exists.

Expected endpoint:

```http
POST /api/pokemon/{pokeApiId}/generate-nicknames
```

Behavior:

```text
Try loading Pokémon from DB.
If DB is available:
    use DB data and refresh cache if needed.

If DB is unavailable:
    load Pokémon from cache.
    Generate AI nickname suggestions if Gemini is available.
    If Gemini fails, use deterministic fallback.
```

If the Pokémon is not found in cache:

```http
404 Not Found
```

or, if the cache is unavailable:

```http
503 Service Unavailable
```

Prefer:

```text
503 when no cache exists due to DB unavailable.
404 only when cache exists but Pokémon is not found.
```

---

# Part 6 — What Must NOT Work Without DB

The following must not use cache:

```text
POST /api/auth/register
POST /api/auth/login
GET /api/auth/me if it reads from DB
GET /api/dream-team
POST /api/dream-team
PUT /api/dream-team/{id}
DELETE /api/dream-team/{id}
GET /api/dream-team/analyze
```

Reason:

```text
These features rely on user-specific or transactional data.
SQL Server is the source of truth.
Cache fallback here would create consistency and security risks.
```

Return:

```http
503 Service Unavailable
```

with:

```json
{
  "errorCode": "DATABASE_UNAVAILABLE",
  "message": "The database is currently unavailable."
}
```

---

# Part 7 — Program.cs Registrations

Expected additions:

```csharp
builder.Services.AddMemoryCache();
builder.Services.AddScoped<PokemonCatalogCacheService>();
```

If a custom DB availability service is added:

```csharp
builder.Services.AddScoped<DatabaseAvailabilityService>();
```

Do not register duplicate services.

---

# Part 8 — Testing

## Test 1 — DB Available

Run:

```http
GET /api/health/db
```

Expected:

```json
{
  "status": "Healthy",
  "database": "Available"
}
```

Then run:

```http
GET /api/pokemon?page=1&pageSize=10
```

Expected:

```text
Returns Pokémon from SQL Server.
Cache is refreshed.
```

---

## Test 2 — DB Unavailable

Stop SQL Server LocalDB or change the connection string temporarily.

Then run:

```http
GET /api/health/db
```

Expected:

```http
503 Service Unavailable
```

```json
{
  "status": "Unhealthy",
  "database": "Unavailable"
}
```

---

## Test 3 — Cache Fallback

Important: first run catalog endpoint while DB is available so cache is warmed.

Then make DB unavailable.

Run:

```http
GET /api/pokemon?page=1&pageSize=10
```

Expected:

```text
Returns Pokémon from cache.
Does not crash.
Does not call PokeAPI.
```

Run:

```http
POST /api/pokemon-smart-search
```

Body:

```json
{
  "query": "find me top 10 small electric pokemon",
  "page": 1,
  "pageSize": 5
}
```

Expected:

```text
Returns results from cache if cache is available.
```

Run:

```http
POST /api/pokemon/25/generate-nicknames
```

Body:

```json
{
  "count": 5
}
```

Expected:

```text
Returns nickname suggestions using cache data.
```

---

## Test 4 — User Data Still Fails Without DB

With DB unavailable, test:

```http
POST /api/auth/login
GET /api/dream-team
POST /api/dream-team
GET /api/dream-team/analyze
```

Expected:

```http
503 Service Unavailable
```

Do not allow writes or user-specific reads from cache.

---

## Test 5 — DB Comes Back

Restore DB connection.

Run:

```http
GET /api/health/db
```

Expected:

```http
200 OK
```

Then run:

```http
GET /api/pokemon?page=1&pageSize=10
```

Expected:

```text
Returns from SQL Server again.
Cache refreshes.
```

No merge is needed because no writes were accepted into cache.

---

# Demo Explanation

Use this explanation in the interview:

```text
For third-party API resilience, PokeAPI is not used during regular user requests. I import the Pokémon catalog on the server side and store it in SQL Server.

For database unavailability, the system returns a clear 503 response for transactional features such as login, registration, and Dream Team management, because SQL Server is the source of truth.

For read-only Pokémon catalog features, I added an in-memory cache fallback. If the DB becomes unavailable after the catalog was already loaded, the user can still browse Pokémon, run Smart Search, and generate nickname suggestions. Once the DB is available again, the system automatically returns to SQL Server and refreshes the cache.

From the user experience perspective, this creates a temporary read-only mode instead of a complete failure. Cached Pokémon browsing can continue, while personal actions are clearly disabled until the database returns.
```

---

# Copilot Prompt

Use this prompt with Copilot:

```text
Read docs/ai-workflow/STEP_07_INSTRUCTIONS_DB_UNAVAILABLE_AND_CATALOG_CACHE.md and implement only this step.

Before changing files:
1. List all files you plan to create or modify.
2. Explain briefly why each file is needed.
3. Wait for my approval.

Rules:
- Do not change unrelated files.
- Keep controllers thin.
- Keep SQL Server as the source of truth.
- Cache is read-only fallback only.
- Do not cache user data.
- Do not support Dream Team writes without DB.
- Do not call PokeAPI from request endpoints.
- Return 503 DATABASE_UNAVAILABLE when DB is unavailable and cache cannot serve the request.
- Also implement the behavior as a backend read-only fallback mode: cache is allowed only for Pokémon catalog/search/nickname features, while all personal or transactional features must return 503 when DB is unavailable.
```
