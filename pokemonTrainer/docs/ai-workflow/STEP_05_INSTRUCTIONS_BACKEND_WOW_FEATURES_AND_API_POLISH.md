# STEP 05 — Backend WOW Features and API Polish

## Purpose

Implement the next backend layer for the Pokémon Trainer API after the core authentication, Pokémon import, Dream Team, Smart Search, Gemini integration, and catalog readiness mechanisms are already in place.

This step focuses on:

1. Dream Team Analyzer
2. AI-assisted team explanation with fallback
3. Pokémon nickname generator
4. Global exception handling middleware
5. Swagger JWT authorization support
6. Optional performance indexes
7. Final backend validation checklist

The goal is to make the backend feel more product-ready, fun, resilient, and demo-friendly.

---

## Current Assumptions

The project already has:

- ASP.NET Core Web API
- SQL Server / LocalDB
- Entity Framework Core
- ASP.NET Identity
- JWT authentication
- ApplicationUser
- Pokémon data imported from PokeAPI through server-side code
- Pokémon persisted locally in SQL Server
- Pokémon stat columns:
  - Hp
  - Attack
  - Defense
  - SpecialAttack
  - SpecialDefense
  - Speed
- DreamTeamPokemon table
- DreamTeamService
- DreamTeamController
- GeminiTextGenerationService
- PokemonSmartSearchService
- Rule-based fallback parser
- PokemonCatalogState readiness mechanism
- Controllers should remain thin
- Business logic should stay inside services

---

# Part 1 — Dream Team Analyzer

## Goal

Add an endpoint that analyzes the authenticated user's Dream Team and returns useful, fun, and structured insights.

The analysis should be based on real data from SQL Server, not invented by AI.

AI may be used only to generate a friendly explanation after deterministic metrics are calculated.

## Endpoint

Add endpoint:

```http
GET /api/dream-team/analyze
```

Requirements:

- Must require authentication.
- Must use the current logged-in user.
- Must respect Pokémon catalog readiness.
- Must analyze only the current user's Dream Team.
- Must return structured analysis even if Gemini is unavailable.
- Must not call PokeAPI.

---

## DTOs

Create folder if needed:

```text
DTOs/DreamTeamAnalysis
```

Create:

```text
DTOs/DreamTeamAnalysis/DreamTeamAnalysisResponse.cs
```

Suggested properties:

```csharp
namespace pokemonTrainer.DTOs.DreamTeamAnalysis;

public class DreamTeamAnalysisResponse
{
    public int MaxTeamSize { get; set; }

    public int CurrentTeamSize { get; set; }

    public bool IsFullTeam { get; set; }

    public int TeamScore { get; set; }

    public List<string> Types { get; set; } = new();

    public List<string> MissingRecommendedTypes { get; set; } = new();

    public TeamAverageStatsResponse AverageStats { get; set; } = new();

    public TeamTopPokemonResponse? FastestPokemon { get; set; }

    public TeamTopPokemonResponse? StrongestPokemon { get; set; }

    public TeamTopPokemonResponse? BestDefensivePokemon { get; set; }

    public List<string> Strengths { get; set; } = new();

    public List<string> Weaknesses { get; set; } = new();

    public List<string> Recommendations { get; set; } = new();

    public string Summary { get; set; } = string.Empty;

    public bool AiSummaryUsed { get; set; }
}
```

Create:

```text
DTOs/DreamTeamAnalysis/TeamAverageStatsResponse.cs
```

```csharp
namespace pokemonTrainer.DTOs.DreamTeamAnalysis;

public class TeamAverageStatsResponse
{
    public double Hp { get; set; }

    public double Attack { get; set; }

    public double Defense { get; set; }

    public double SpecialAttack { get; set; }

    public double SpecialDefense { get; set; }

    public double Speed { get; set; }

    public double TotalStats { get; set; }
}
```

Create:

```text
DTOs/DreamTeamAnalysis/TeamTopPokemonResponse.cs
```

```csharp
namespace pokemonTrainer.DTOs.DreamTeamAnalysis;

public class TeamTopPokemonResponse
{
    public int PokeApiId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Nickname { get; set; }

    public string? ImageUrl { get; set; }

    public int Value { get; set; }
}
```

---

## Service

Create:

```text
Services/DreamTeamAnalysisService.cs
```

Responsibilities:

- Load the authenticated user's Dream Team from SQL Server.
- Include Pokémon and Pokémon types.
- Calculate deterministic metrics.
- Optionally ask Gemini to write a friendly trainer-style summary.
- Fall back to a deterministic summary if Gemini fails.

Constructor dependencies:

```csharp
ApplicationDbContext
GeminiTextGenerationService
ILogger<DreamTeamAnalysisService>
```

Public method:

```csharp
Task<DreamTeamAnalysisResponse> AnalyzeAsync(
    string userId,
    CancellationToken cancellationToken = default)
```

---

## Deterministic Analysis Rules

Use real DB data.

### Team size

```text
CurrentTeamSize = number of selected Pokémon
MaxTeamSize = 5
IsFullTeam = CurrentTeamSize == 5
```

### Average stats

Calculate average:

- Hp
- Attack
- Defense
- SpecialAttack
- SpecialDefense
- Speed
- TotalStats = Hp + Attack + Defense + SpecialAttack + SpecialDefense + Speed

If the team is empty, return zeros and a recommendation to add Pokémon.

### Top Pokémon

Find:

- FastestPokemon by Speed
- StrongestPokemon by Attack
- BestDefensivePokemon by Defense

### Types

Collect all distinct Pokémon types in the team.

Recommended type coverage list:

```text
fire, water, electric, grass, ground, flying, psychic, dragon
```

MissingRecommendedTypes should be the recommended types not currently represented.

### Strengths

Examples:

- If average Speed >= 80: `This team is fast and can act quickly.`
- If average Attack >= 80: `This team has strong physical attack potential.`
- If average Defense >= 75: `This team has solid defensive presence.`
- If team has at least 4 distinct types: `This team has good type variety.`
- If CurrentTeamSize == 5: `This is a full Dream Team.`

### Weaknesses

Examples:

- If CurrentTeamSize < 5: `The team is not full yet.`
- If average Speed < 50: `The team may be slow compared to faster opponents.`
- If average Defense < 50: `The team may struggle defensively.`
- If there are fewer than 3 distinct types: `The team has limited type variety.`
- If no water/grass/electric/fire coverage exists, recommend adding variety.

### Recommendations

Examples:

- If team size is less than 5: `Add more Pokémon to complete your Dream Team.`
- If missing water: `Consider adding a Water type for better balance.`
- If missing electric: `Consider adding an Electric type for speed and coverage.`
- If low defense: `Consider adding a defensive Pokémon with higher Defense or HP.`
- If low speed: `Consider adding a faster Pokémon to improve initiative.`

### Team score

Calculate a score from 0 to 100.

Suggested approach:

- Start at 40.
- Add up to 20 points for team size:
  - `(CurrentTeamSize / 5.0) * 20`
- Add up to 20 points for type variety:
  - `min(distinctTypeCount, 5) / 5.0 * 20`
- Add up to 20 points for average total stats:
  - use average total stats threshold, for example:
    - `averageTotalStats >= 500 => 20`
    - otherwise proportional

Clamp to 0–100.

---

## AI Summary

Use Gemini only after deterministic analysis is calculated.

Create a prompt that includes:

- Team size
- Types
- Average stats
- Strengths
- Weaknesses
- Recommendations
- Team score

Gemini should return a short friendly summary only.

Rules:

- Do not allow Gemini to invent Pokémon.
- Do not ask Gemini to calculate stats.
- Do not let Gemini replace deterministic logic.
- If Gemini fails, use fallback summary.
- Set `AiSummaryUsed = true` only if Gemini was actually used.

Fallback summary example:

```text
Your Dream Team has a score of 76/100. It has good Electric coverage and strong Speed, but could be improved by adding more defensive variety.
```

---

## Controller Update

Update:

```text
Controllers/DreamTeamController.cs
```

Inject:

```csharp
DreamTeamAnalysisService
```

Add endpoint:

```csharp
[HttpGet("analyze")]
public async Task<IActionResult> Analyze(
    CancellationToken cancellationToken = default)
```

Behavior:

- Check `_statusService.IsReady`.
- Get current user id.
- If user id is missing, return Unauthorized.
- Call `_dreamTeamAnalysisService.AnalyzeAsync(userId, cancellationToken)`.
- Return Ok(response).

Keep controller thin.

---

## Program.cs Registration

Add:

```csharp
builder.Services.AddScoped<DreamTeamAnalysisService>();
```

---

# Part 2 — Nickname Generator

## Goal

Add a fun AI-powered nickname suggestion feature.

The user can request nickname suggestions for a Pokémon.

The feature should work with Gemini when available and use deterministic fallback when unavailable.

## Endpoint Option A

Add to Pokémon controller:

```http
POST /api/pokemon/{pokeApiId}/generate-nicknames
```

This is good because nicknames can be generated before adding the Pokémon to the Dream Team.

## DTOs

Create folder if needed:

```text
DTOs/Nicknames
```

Create:

```text
DTOs/Nicknames/GeneratePokemonNicknamesRequest.cs
```

```csharp
using System.ComponentModel.DataAnnotations;

namespace pokemonTrainer.DTOs.Nicknames;

public class GeneratePokemonNicknamesRequest
{
    [Range(1, 10)]
    public int Count { get; set; } = 5;
}
```

Create:

```text
DTOs/Nicknames/GeneratePokemonNicknamesResponse.cs
```

```csharp
namespace pokemonTrainer.DTOs.Nicknames;

public class GeneratePokemonNicknamesResponse
{
    public int PokeApiId { get; set; }

    public string PokemonName { get; set; } = string.Empty;

    public List<string> Types { get; set; } = new();

    public List<string> Suggestions { get; set; } = new();

    public bool AiUsed { get; set; }
}
```

---

## Service

Create:

```text
Services/PokemonNicknameService.cs
```

Constructor dependencies:

```csharp
ApplicationDbContext
GeminiTextGenerationService
ILogger<PokemonNicknameService>
```

Public method:

```csharp
Task<ServiceResult<GeneratePokemonNicknamesResponse>> GenerateAsync(
    int pokeApiId,
    int count,
    CancellationToken cancellationToken = default)
```

Behavior:

1. Load Pokémon from local DB by PokeApiId.
2. Include Pokémon types.
3. If not found, return ServiceResult fail with `POKEMON_NOT_FOUND`.
4. Try Gemini:
   - Ask for JSON array of nickname strings.
   - Count should be clamped to 1–10.
   - Names should be short, fun, and family-friendly.
   - No explanations.
5. If Gemini fails:
   - Use deterministic fallback names.

Fallback examples:

For electric:

```text
Sparky, Volt, Thunder, Zappy, Bolt
```

For fire:

```text
Blaze, Ember, Flame, Scorch, Inferno
```

For water:

```text
Splash, Aqua, Wave, Bubbles, Tide
```

Default:

```text
Buddy, Champ, Hero, Scout, Ace
```

Return:

- PokeApiId
- PokemonName
- Types
- Suggestions
- AiUsed

---

## Controller Update

Update:

```text
Controllers/PokemonController.cs
```

Inject:

```csharp
PokemonNicknameService
```

Add endpoint:

```csharp
[Authorize]
[HttpPost("{pokeApiId:int}/generate-nicknames")]
public async Task<IActionResult> GenerateNicknames(
    int pokeApiId,
    GeneratePokemonNicknamesRequest request,
    CancellationToken cancellationToken = default)
```

Behavior:

- Check `_statusService.IsReady`.
- Call service.
- If not success, map `POKEMON_NOT_FOUND` to NotFound.
- Return Ok(response).

Keep controller thin.

---

## Program.cs Registration

Add:

```csharp
builder.Services.AddScoped<PokemonNicknameService>();
```

---

# Part 3 — Global Error Handling Middleware

## Goal

Add consistent handling for unhandled exceptions.

Do not leak internal exception details to API consumers.

## Create Middleware

Create folder:

```text
Middleware
```

Create:

```text
Middleware/GlobalExceptionHandlingMiddleware.cs
```

Behavior:

- Catch unhandled exceptions.
- Log the full exception.
- Return HTTP 500.
- Return JSON:

```json
{
  "errorCode": "INTERNAL_SERVER_ERROR",
  "message": "An unexpected error occurred."
}
```

In Development, optionally include a `details` field, but do not include it in Production.

## Program.cs

Register near the top of the pipeline, before authentication/authorization:

```csharp
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

Recommended order:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

---

# Part 4 — Swagger JWT Authorization

## Goal

Allow testing secured endpoints directly from Swagger UI.

## Program.cs

Update `AddSwaggerGen` to include JWT Bearer security definition.

Add using statements:

```csharp
using Microsoft.OpenApi.Models;
```

Configure:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pokemon Trainer API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

Make sure existing Swagger setup is not duplicated.

---

# Part 5 — Optional Performance Indexes

## Goal

Improve sorting/filtering performance for Smart Search.

Update `ApplicationDbContext.OnModelCreating`.

Add indexes:

```csharp
builder.Entity<Pokemon>()
    .HasIndex(p => p.Speed);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Attack);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Defense);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Hp);

builder.Entity<Pokemon>()
    .HasIndex(p => p.BaseExperience);
```

Optional:

```csharp
builder.Entity<Pokemon>()
    .HasIndex(p => p.Height);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Weight);
```

Then run:

```powershell
Add-Migration AddPokemonSearchIndexes
Update-Database
```

Do not over-index unnecessarily, but these are reasonable for demo and search usage.

---

# Validation Checklist

## Dream Team Analyzer

Test:

```http
GET /api/dream-team/analyze
Authorization: Bearer {token}
```

Expected:

- Returns team size
- Returns types
- Returns average stats
- Returns strongest / fastest / defensive Pokémon
- Returns strengths
- Returns weaknesses
- Returns recommendations
- Returns score 0–100
- Works even if Gemini is unavailable
- `AiSummaryUsed` correctly reflects whether Gemini was used

## Nickname Generator

Test:

```http
POST /api/pokemon/25/generate-nicknames
Authorization: Bearer {token}
```

Body:

```json
{
  "count": 5
}
```

Expected:

- Returns Pikachu nickname suggestions
- Uses Gemini if available
- Falls back to deterministic suggestions if Gemini fails
- Does not expose Gemini errors to the client

## Error Middleware

Temporarily throw an exception from a test endpoint or service.

Expected:

```json
{
  "errorCode": "INTERNAL_SERVER_ERROR",
  "message": "An unexpected error occurred."
}
```

## Swagger JWT

- Run the API.
- Open Swagger.
- Login through `/api/auth/login`.
- Copy token.
- Click Authorize.
- Paste:

```text
Bearer {token}
```

- Test secured endpoints.

---

# Design Explanation for Reviewers

The Dream Team Analyzer uses deterministic calculations based on SQL Server data and Pokémon stats. AI is used only to produce a friendly trainer-style summary. This prevents the AI from inventing facts and keeps the database as the source of truth.

The Nickname Generator is intentionally low-risk and fun. It uses Gemini when available and falls back to deterministic nickname suggestions when Gemini is unavailable.

The Global Error Middleware improves reliability and creates consistent API errors.

Swagger JWT support improves demo and testing experience.

Search indexes improve performance for the Smart Search scenarios.
