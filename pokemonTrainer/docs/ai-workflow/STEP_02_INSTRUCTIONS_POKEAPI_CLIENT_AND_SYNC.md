# Step 02 Instructions — PokeAPI Client and Pokémon Sync

## Purpose

Implement the Pokémon data import foundation.

This step adds a server-side PokeAPI integration and a synchronization service that imports Pokémon data into the local SQL Server database.

The client must never call PokeAPI directly. All external data access must go through the ASP.NET Core Web API backend.

Work incrementally. After each phase, stop and wait for approval before continuing.

---

## Target Result

At the end of this step, the backend should include:

- A typed `PokeApiClient`.
- DTOs that represent only the PokeAPI response fields needed by the application.
- A `PokemonSyncService`.
- A protected or development-only sync endpoint.
- Ability to import a limited number of Pokémon from PokeAPI into SQL Server.
- Imported data saved into:
  - `Pokemons`
  - `PokemonTypes`
  - `PokemonPokemonTypes`
- Basic duplicate prevention using `PokeApiId`.
- Basic error handling for external API failures.
- Validation that imported Pokémon can be queried from the database.

---

## Expected Project Structure Additions

Add the following files:

```text
pokemonTrainer/
│
├── Controllers/
│   └── PokemonSyncController.cs
│
├── DTOs/
│   └── PokeApi/
│       ├── PokeApiListResponse.cs
│       ├── PokeApiNamedResource.cs
│       ├── PokeApiPokemonDetails.cs
│       ├── PokeApiPokemonTypeSlot.cs
│       ├── PokeApiPokemonStatSlot.cs
│       └── PokeApiPokemonAbilitySlot.cs
│
├── Infrastructure/
│   └── PokeApiClient.cs
│
└── Services/
    └── PokemonSyncService.cs
```

---

# Phase 1 — Add PokeAPI DTOs

## Goal

Create DTOs for deserializing only the required fields from PokeAPI.

Do not model the entire PokeAPI response.

Only include fields required for the current import:

- Pokémon ID
- Name
- Height
- Weight
- Base experience
- Types
- Stats
- Abilities
- Image URL

## Create Folder

```text
DTOs/PokeApi
```

## Create PokeApiNamedResource.cs

```csharp
namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiNamedResource
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
}
```

## Create PokeApiListResponse.cs

```csharp
namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiListResponse
{
    public int Count { get; set; }

    public string? Next { get; set; }

    public string? Previous { get; set; }

    public List<PokeApiNamedResource> Results { get; set; } = new();
}
```

## Create PokeApiPokemonTypeSlot.cs

```csharp
namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiPokemonTypeSlot
{
    public int Slot { get; set; }

    public PokeApiNamedResource Type { get; set; } = new();
}
```

## Create PokeApiPokemonStatSlot.cs

```csharp
using System.Text.Json.Serialization;

namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiPokemonStatSlot
{
    [JsonPropertyName("base_stat")]
    public int BaseStat { get; set; }

    public int Effort { get; set; }

    public PokeApiNamedResource Stat { get; set; } = new();
}
```

## Create PokeApiPokemonAbilitySlot.cs

```csharp
using System.Text.Json.Serialization;

namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiPokemonAbilitySlot
{
    [JsonPropertyName("is_hidden")]
    public bool IsHidden { get; set; }

    public int Slot { get; set; }

    public PokeApiNamedResource Ability { get; set; } = new();
}
```

## Create PokeApiPokemonSprites.cs

```csharp
using System.Text.Json.Serialization;

namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiPokemonSprites
{
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }

    [JsonPropertyName("other")]
    public PokeApiPokemonSpriteOther? Other { get; set; }
}

public class PokeApiPokemonSpriteOther
{
    [JsonPropertyName("official-artwork")]
    public PokeApiPokemonOfficialArtwork? OfficialArtwork { get; set; }
}

public class PokeApiPokemonOfficialArtwork
{
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }
}

```
## Image Handling Strategy

For the initial implementation, store only the Pokémon image URL returned by PokeAPI.

Prefer the official artwork image when available:

```text
sprites.other.official-artwork.front_default
```

If official artwork is not available, fall back to:

```text
sprites.front_default
```

Do not download images into the backend or store them in the database at this stage.

The frontend should later handle broken image URLs by displaying a local placeholder image.

For a production-grade implementation, image caching or storing images in Blob Storage / CDN could be added later to reduce runtime dependency on external image URLs and improve reliability.

## Create PokeApiPokemonDetails.cs

```csharp
using System.Text.Json.Serialization;

namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiPokemonDetails
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Height { get; set; }

    public int Weight { get; set; }

    [JsonPropertyName("base_experience")]
    public int? BaseExperience { get; set; }

    public List<PokeApiPokemonTypeSlot> Types { get; set; } = new();

    public List<PokeApiPokemonStatSlot> Stats { get; set; } = new();

    public List<PokeApiPokemonAbilitySlot> Abilities { get; set; } = new();

    public PokeApiPokemonSprites Sprites { get; set; } = new();
}
```

## Validation

Build the solution.

Stop and wait for approval.

---

# Phase 2 — Add PokeApiClient

## Goal

Create a typed client for communicating with PokeAPI from the server.

## File Location

```text
Infrastructure/PokeApiClient.cs
```

## Code

```csharp
using System.Net.Http.Json;
using pokemonTrainer.DTOs.PokeApi;

namespace pokemonTrainer.Infrastructure;

public class PokeApiClient
{
    private readonly HttpClient _httpClient;

    public PokeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PokeApiListResponse?> GetPokemonListAsync(int limit, int offset = 0)
    {
        return await _httpClient.GetFromJsonAsync<PokeApiListResponse>(
            $"pokemon?limit={limit}&offset={offset}");
    }

    public async Task<PokeApiPokemonDetails?> GetPokemonDetailsAsync(string nameOrId)
    {
        return await _httpClient.GetFromJsonAsync<PokeApiPokemonDetails>(
            $"pokemon/{nameOrId}");
    }
}
```

## Register HttpClient in Program.cs

```csharp
using pokemonTrainer.Infrastructure;
```

Register:

```csharp
builder.Services.AddHttpClient<PokeApiClient>(client =>
{
    client.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
    client.Timeout = TimeSpan.FromSeconds(20);
});
```

## Validation

Build the solution.

Stop and wait for approval.

---

# Phase 3 — Add PokemonSyncService

## Goal

Create a service that imports Pokémon from PokeAPI into SQL Server.

## File Location

```text
Services/PokemonSyncService.cs
```

## Implementation Requirements

The service should:

1. Request a list of Pokémon from PokeAPI.
2. For each Pokémon, request detailed data.
3. Skip Pokémon that already exist by `PokeApiId`.
4. Create missing Pokémon types.
5. Save Pokémon details.
6. Save many-to-many type links.
7. Store stats and abilities as JSON.
8. Save changes to SQL Server.
9. Return a summary of the import.

## Important Notes

- Keep the first sync limited, for example `limit=20`.
- Do not import all Pokémon in the first implementation.
- Avoid failing the entire sync if one Pokémon fails.
- Basic error handling is acceptable in this phase.
- More advanced retry/circuit-breaker behavior can be added later.

Stop and wait for approval after implementation.

---

# Phase 4 — Add PokemonSyncController

## Goal

Expose a temporary development endpoint to trigger Pokémon sync manually.

## File Location

```text
Controllers/PokemonSyncController.cs
```

## Endpoint

```http
POST /api/pokemon-sync?limit=20
```

## Requirements

- Accept a `limit` query parameter.
- Default to 20.
- Cap the maximum limit to avoid very large imports during development.
- Call `PokemonSyncService`.
- Return a sync summary.

## Example Response

```json
{
  "requested": 20,
  "created": 20,
  "skipped": 0,
  "failed": 0
}
```

Stop and wait for approval.

---

# Phase 5 — Validate Database Import

## Goal

Confirm that imported Pokémon were saved correctly.

## Validation

Check the database tables:

```text
Pokemons
PokemonTypes
PokemonPokemonTypes
```

Expected result:

- `Pokemons` contains imported Pokémon.
- `PokemonTypes` contains types such as grass, fire, water, electric.
- `PokemonPokemonTypes` contains many-to-many relationships.

If import fails, inspect:

- API URL
- Network connection
- JSON DTO mappings
- Database constraints
- Duplicate records

Stop and wait for approval.

---

# Final Step Summary Format

At the end of the step, provide:

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
