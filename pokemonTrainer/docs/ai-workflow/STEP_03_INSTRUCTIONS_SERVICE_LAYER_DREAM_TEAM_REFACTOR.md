# Step 03 Instructions —  Backend Service Layer, Dream Team, and Controller Refactor

## Purpose

The main goal of this step is to keep controllers thin and move business logic into dedicated services.

Controllers should be responsible only for HTTP concerns:

- Routing
- Authorization attributes
- Reading the authenticated user from JWT claims
- Calling application services
- Translating service results into HTTP responses

Business logic should live in services:

- Validation rules
- Database queries
- Mapping to response DTOs
- Pagination
- Filtering
- Dream Team rules
- Authentication flow
- JWT response construction

This stage builds on the existing backend foundation:

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server LocalDB
- ASP.NET Identity
- JWT authentication
- PokeAPI import worker
- Pokémon data stored locally in SQL Server
- Import readiness/status service

---

## Architecture Rule for This Step

Use the following separation of concerns consistently:

```text
Controller = HTTP layer
Service = Business logic / application logic
DbContext = Persistence
DTOs = API contracts
Models = Database entities
```

Avoid placing business rules directly inside controllers.

Examples of logic that should not stay in controllers:

- Dream Team maximum size validation
- Duplicate Pokémon validation
- Slot allocation
- Pokémon search/filter logic
- Pagination calculations
- JSON parsing for stats and abilities
- Auth registration/login flow
- Last login update
- JWT response construction

---

# Phase 1 — Common Service Result

## Goal

Create a reusable result wrapper for service operations.

Services should not return `IActionResult`, because that would couple business logic to the HTTP layer.

Instead, services should return a structured result object containing:

- Success flag
- Error code
- Message
- Data

The controller converts this result into the appropriate HTTP response.

## File Created

```text
DTOs/Common/ServiceResult.cs
```

## Implementation

```csharp
namespace pokemonTrainer.DTOs.Common;

public class ServiceResult<T>
{
    public bool Success { get; set; }

    public string? ErrorCode { get; set; }

    public string? Message { get; set; }

    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data)
    {
        return new ServiceResult<T>
        {
            Success = true,
            Data = data
        };
    }

    public static ServiceResult<T> Fail(
        string errorCode,
        string message)
    {
        return new ServiceResult<T>
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}
```

---

# Phase 2 — Dream Team Database Model

## Goal

Add a personal Dream Team feature.

Each authenticated user can maintain a team of up to five Pokémon.

Business rules:

- A user can have up to 5 Pokémon in the Dream Team.
- A user cannot add the same Pokémon twice.
- Each Pokémon receives a slot from 1 to 5.
- Each user only sees and modifies their own Dream Team.
- Nickname is optional and limited to 50 characters.

## File Created

```text
Models/DreamTeamPokemon.cs
```

## Implementation

```csharp
namespace pokemonTrainer.Models;

public class DreamTeamPokemon
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int PokemonId { get; set; }

    public Pokemon Pokemon { get; set; } = null!;

    public int Slot { get; set; }

    public string? Nickname { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

## ApplicationDbContext Updates

Added:

```csharp
public DbSet<DreamTeamPokemon> DreamTeamPokemons { get; set; }
```

Added model configuration inside `OnModelCreating`:

```csharp
builder.Entity<DreamTeamPokemon>()
    .HasIndex(d => new { d.UserId, d.PokemonId })
    .IsUnique();

builder.Entity<DreamTeamPokemon>()
    .HasIndex(d => new { d.UserId, d.Slot })
    .IsUnique();

builder.Entity<DreamTeamPokemon>()
    .Property(d => d.Nickname)
    .HasMaxLength(50);

builder.Entity<DreamTeamPokemon>()
    .HasOne(d => d.User)
    .WithMany()
    .HasForeignKey(d => d.UserId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Entity<DreamTeamPokemon>()
    .HasOne(d => d.Pokemon)
    .WithMany()
    .HasForeignKey(d => d.PokemonId)
    .OnDelete(DeleteBehavior.Restrict);
```

## Migration Executed

```powershell
Add-Migration AddDreamTeamPokemons
Update-Database
```

---

# Phase 3 — Dream Team DTOs

## Goal

Create request and response DTOs for the Dream Team API.

## Files Created

```text
DTOs/DreamTeam/AddDreamTeamPokemonRequest.cs
DTOs/DreamTeam/UpdateDreamTeamPokemonRequest.cs
DTOs/DreamTeam/DreamTeamPokemonResponse.cs
DTOs/DreamTeam/DreamTeamResponse.cs
```

## DTOs

```csharp
using System.ComponentModel.DataAnnotations;

namespace pokemonTrainer.DTOs.DreamTeam;

public class AddDreamTeamPokemonRequest
{
    [Required]
    public int PokeApiId { get; set; }

    [MaxLength(50)]
    public string? Nickname { get; set; }
}
```

```csharp
using System.ComponentModel.DataAnnotations;

namespace pokemonTrainer.DTOs.DreamTeam;

public class UpdateDreamTeamPokemonRequest
{
    [MaxLength(50)]
    public string? Nickname { get; set; }
}
```

```csharp
namespace pokemonTrainer.DTOs.DreamTeam;

public class DreamTeamPokemonResponse
{
    public int Id { get; set; }

    public int Slot { get; set; }

    public string? Nickname { get; set; }

    public int PokeApiId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int Height { get; set; }

    public int Weight { get; set; }

    public int? BaseExperience { get; set; }

    public bool IsLegendary { get; set; }

    public List<string> Types { get; set; } = new();
}
```

```csharp
namespace pokemonTrainer.DTOs.DreamTeam;

public class DreamTeamResponse
{
    public int MaxSize { get; set; } = 5;

    public int CurrentSize { get; set; }

    public List<DreamTeamPokemonResponse> Items { get; set; } = new();
}
```

---

# Phase 4 — DreamTeamService

## Goal

Implement all Dream Team business logic inside a dedicated service.

The controller should not contain the rules for:

- Maximum team size
- Duplicate Pokémon validation
- Slot allocation
- Nickname normalization
- Database persistence
- Response mapping

## File Created

```text
Services/DreamTeamService.cs
```

## Responsibilities

`DreamTeamService` exposes:

```csharp
Task<DreamTeamResponse> GetMyTeamAsync(
    string userId,
    CancellationToken cancellationToken = default)

Task<ServiceResult<DreamTeamPokemonResponse>> AddPokemonAsync(
    string userId,
    AddDreamTeamPokemonRequest request,
    CancellationToken cancellationToken = default)

Task<ServiceResult<DreamTeamPokemonResponse>> UpdateNicknameAsync(
    string userId,
    int dreamTeamPokemonId,
    UpdateDreamTeamPokemonRequest request,
    CancellationToken cancellationToken = default)

Task<ServiceResult<bool>> RemovePokemonAsync(
    string userId,
    int dreamTeamPokemonId,
    CancellationToken cancellationToken = default)
```

## Business Rules Implemented

When adding a Pokémon:

1. Load the current team for the authenticated user.
2. If the team already has 5 Pokémon, return `TEAM_FULL`.
3. Find the Pokémon by `PokeApiId`.
4. If it does not exist locally, return `POKEMON_NOT_FOUND`.
5. If the user already added that Pokémon, return `POKEMON_ALREADY_EXISTS`.
6. Find the next available slot between 1 and 5.
7. Save the new Dream Team row.
8. Return the mapped response.

When updating:

1. Find the Dream Team row by ID and authenticated user.
2. If not found, return `DREAM_TEAM_POKEMON_NOT_FOUND`.
3. Normalize and update nickname.
4. Save changes.

When deleting:

1. Find the Dream Team row by ID and authenticated user.
2. If not found, return `DREAM_TEAM_POKEMON_NOT_FOUND`.
3. Remove it.
4. Save changes.

## Security Rule

The service receives `userId` from the controller.

The client never sends `userId` in the request body.

The authenticated user is resolved from JWT claims in the controller.

---

# Phase 5 — Thin DreamTeamController

## Goal

Create a thin controller that delegates all Dream Team logic to `DreamTeamService`.

## File Replaced

```text
Controllers/DreamTeamController.cs
```

## Required Behavior

The controller:

- Uses `[Authorize]`
- Uses route `api/dream-team`
- Reads the authenticated user ID from JWT claims
- Checks Pokémon import readiness before returning Pokémon-dependent data
- Calls `DreamTeamService`
- Converts `ServiceResult<T>` into HTTP responses

## Endpoints

```http
GET    /api/dream-team
POST   /api/dream-team
PUT    /api/dream-team/{id}
DELETE /api/dream-team/{id}
```

## HTTP Mapping

```text
POKEMON_NOT_FOUND              -> 404 Not Found
DREAM_TEAM_POKEMON_NOT_FOUND   -> 404 Not Found
TEAM_FULL                      -> 400 Bad Request
POKEMON_ALREADY_EXISTS         -> 400 Bad Request
```

## Readiness Guard

If Pokémon data is not ready, return:

```http
503 Service Unavailable
```

With response:

```json
{
  "status": "ImportInProgress",
  "message": "Pokémon data is still loading. Please try again shortly."
}
```

---

# Phase 6 — Pokémon DTOs

## Goal

Create API response DTOs for Pokémon list, details, stats, abilities, and pagination.

## Files Created

```text
DTOs/Pokemon/PokemonListItemResponse.cs
DTOs/Pokemon/PokemonPagedResponse.cs
DTOs/Pokemon/PokemonStatResponse.cs
DTOs/Pokemon/PokemonAbilityResponse.cs
DTOs/Pokemon/PokemonDetailsResponse.cs
```

## Supported Endpoints

```http
GET /api/pokemon?page=1&pageSize=20
GET /api/pokemon?search=pika
GET /api/pokemon?type=fire
GET /api/pokemon/25
GET /api/pokemon/types
```

---

# Phase 7 — PokemonService

## Goal

Move Pokémon read/query logic out of `PokemonController` into `PokemonService`.

`PokemonController` should not contain:

- Search filtering
- Type filtering
- Pagination calculations
- EF query construction
- Mapping
- Stats JSON parsing
- Abilities JSON parsing

## File Created

```text
Services/PokemonService.cs
```

## Responsibilities

`PokemonService` exposes:

```csharp
Task<PokemonPagedResponse> GetPagedAsync(
    string? search,
    string? type,
    int page = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default)

Task<List<string>> GetTypesAsync(
    CancellationToken cancellationToken = default)

Task<ServiceResult<PokemonDetailsResponse>> GetByPokeApiIdAsync(
    int pokeApiId,
    CancellationToken cancellationToken = default)
```

## Query Rules

Pagination:

```text
page minimum = 1
pageSize minimum = 1
pageSize maximum = 100
```

Search:

- If search is numeric, match by `PokeApiId` or name.
- Otherwise, search by Pokémon name using SQL LIKE.

Type filter:

- Match by exact type name after trimming and lowercasing.

Details:

- Load Pokémon by `PokeApiId`.
- Include Pokémon types.
- Parse `StatsJson` into `PokemonStatResponse`.
- Parse `AbilitiesJson` into `PokemonAbilityResponse`.
- If not found, return `POKEMON_NOT_FOUND`.

---

# Phase 8 — Thin PokemonController

## Goal

Replace the existing Pokémon controller with a thin HTTP layer.

## File Replaced

```text
Controllers/PokemonController.cs
```

## Endpoints

```http
GET /api/pokemon
GET /api/pokemon/types
GET /api/pokemon/{pokeApiId}
```

## Required Behavior

The controller:

- Checks Pokémon import readiness.
- Calls `PokemonService`.
- Returns `Ok(...)` on success.
- Returns `404 Not Found` for `POKEMON_NOT_FOUND`.
- Avoids direct EF queries.
- Avoids mapping logic.
- Avoids JSON parsing.

---

# Phase 9 — AuthService

## Goal

Move authentication flow out of `AuthController` into `AuthService`.

`AuthController` should not contain:

- Existing user lookup logic
- Password validation logic
- User creation logic
- Last login update
- JWT response construction

## File Created

```text
Services/AuthService.cs
```

## Responsibilities

`AuthService` exposes:

```csharp
Task<ServiceResult<UserInfoResponse>> RegisterAsync(
    RegisterRequest request)

Task<ServiceResult<AuthResponse>> LoginAsync(
    LoginRequest request)
```

## Register Flow

1. Check if email already exists.
2. If exists, return `EMAIL_ALREADY_EXISTS`.
3. Create `ApplicationUser`.
4. Use `UserManager.CreateAsync`.
5. If failed, return `REGISTRATION_FAILED`.
6. Return `UserInfoResponse`.

## Login Flow

1. Find user by email.
2. If not found, return `INVALID_CREDENTIALS`.
3. Validate password.
4. If invalid, return `INVALID_CREDENTIALS`.
5. Update `LastLoginAt`.
6. Persist it using `UserManager.UpdateAsync`.
7. Create JWT using `JwtTokenService`.
8. Return `AuthResponse`.

## Important Fix

Ensure this is actually persisted:

```csharp
user.LastLoginAt = DateTime.UtcNow;
await _userManager.UpdateAsync(user);
```

---

# Phase 10 — Thin AuthController

## Goal

Refactor `AuthController` to delegate authentication logic to `AuthService`.

## File Replaced

```text
Controllers/AuthController.cs
```

## Endpoints

```http
POST /api/auth/register
POST /api/auth/login
GET  /api/auth/me
```

## Required Behavior

The controller:

- Calls `AuthService.RegisterAsync`
- Calls `AuthService.LoginAsync`
- Converts `ServiceResult<T>` into HTTP responses
- Keeps `/me` protected with `[Authorize]`
- Reads claims from JWT for `/me`

## HTTP Mapping

```text
EMAIL_ALREADY_EXISTS   -> 400 Bad Request
REGISTRATION_FAILED    -> 400 Bad Request
INVALID_CREDENTIALS    -> 401 Unauthorized
LOGIN_UPDATE_FAILED    -> 400 Bad Request
```

---

# Phase 11 — Development JWT Expiration

## Goal

Improve local development experience by making JWT tokens last longer in the Development environment.

Do not remove token expiration entirely.

Use a long development expiration of 30 days.

## File Updated

```text
appsettings.Development.json
```

## Implementation

```json
{
  "Jwt": {
    "ExpiresInMinutes": 43200
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

## Explanation

`43200` minutes equals 30 days.

The base JWT settings remain in `appsettings.json`:

- Key
- Issuer
- Audience
- Default expiration

Development only overrides:

```text
Jwt:ExpiresInMinutes
```

After changing this setting, perform login again to receive a new token with the longer expiration.

JWT remains valid after server restart as long as:

- The signing key does not change.
- Issuer and audience do not change.
- The token has not expired.

---

# Phase 12 — Program.cs Service Registration

## Goal

Register all new services in dependency injection.

## Registrations Added

```csharp
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PokemonService>();
builder.Services.AddScoped<DreamTeamService>();
```

These exist alongside the previously configured services:

```csharp
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddSingleton<PokemonImportStatusService>();
builder.Services.AddScoped<PokemonImportService>();
builder.Services.AddHostedService<PokemonStartupImportWorker>();
```

---

# Validation Checklist

## Pokémon API

Validate:

```http
GET /api/pokemon?page=1&pageSize=5
GET /api/pokemon/25
GET /api/pokemon/types
GET /api/pokemon/999999
```

Expected:

- Pokémon list returns paged data.
- Pokémon 25 returns Pikachu.
- Types endpoint returns available Pokémon types.
- Invalid Pokémon ID returns `POKEMON_NOT_FOUND`.

## Dream Team API

Use a valid JWT token.

Validate:

```http
GET    /api/dream-team
POST   /api/dream-team
PUT    /api/dream-team/{id}
DELETE /api/dream-team/{id}
```

Expected:

- Empty team returns `currentSize = 0`.
- Adding Pikachu works.
- Adding the same Pokémon again returns `POKEMON_ALREADY_EXISTS`.
- Updating nickname works.
- Deleting a Dream Team Pokémon returns `204 No Content`.

## Auth API

Validate:

```http
POST /api/auth/register
POST /api/auth/login
GET  /api/auth/me
```

Expected:

- Register creates a user.
- Login returns JWT token and user info.
- Invalid login returns `INVALID_CREDENTIALS`.
- `/me` works only with a valid bearer token.
- `LastLoginAt` is updated in `AspNetUsers`.

SQL check:

```sql
SELECT Email, DisplayName, LastLoginAt
FROM AspNetUsers;
```

## Development JWT Expiration

Validate:

1. Run in Development environment.
2. Perform login.
3. Check that `expiresAt` is approximately 30 days in the future.
4. Restart the API.
5. Reuse the same JWT token.
6. Confirm protected endpoints still work.

---

# Final Step Summary

After completing this step, the backend includes:

- Thin controllers
- Dedicated service layer
- Reusable `ServiceResult<T>`
- Dream Team database model
- Dream Team business rules in `DreamTeamService`
- Pokémon query logic in `PokemonService`
- Authentication flow in `AuthService`
- Persisted `LastLoginAt`
- Long-lived Development JWT tokens
- Clean separation between HTTP, business logic, persistence, and API contracts

This stage establishes a maintainable backend structure suitable for continuing with:

- User-triggered Pokémon import check
- AI search
- Team analysis
- Angular UI integration
