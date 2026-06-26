# Copilot Instructions — PokéTrainer AI

## Purpose

This file defines the development guidelines for the PokéTrainer AI project.

The project should be implemented as a clean, maintainable full-stack application with a clear separation of concerns, reliable backend behavior, polished user experience, and meaningful AI-assisted features.

The application allows users to register as Pokémon trainers, explore Pokémon data, create a personal dream team, and use AI-assisted search and recommendations to improve their team.

---

## Core Development Workflow

Development must be done incrementally.

Do not generate or change large parts of the application in a single step.

For each step:

1. Define the goal of the step.
2. List the files that will be created or modified.
3. Implement only the required scope.
4. Explain the design decision briefly.
5. Provide a clear validation method.
6. Stop and wait for approval before moving to the next step.

Do not continue to the next implementation phase without explicit approval.

---

## Technology Stack

### Backend

- ASP.NET Core Web API
- .NET 10 LTS
- Controllers-based API
- Entity Framework Core
- SQL Server / SQL Server LocalDB for local development
- ASP.NET Core Identity
- JWT Bearer Authentication
- Swagger/OpenAPI for development-time API exploration
- Dependency Injection
- Async/await for I/O operations
- Structured services for business logic

### Frontend

- Angular
- TypeScript
- Standalone components where appropriate
- Reactive Forms
- Route Guards
- HTTP Interceptors
- Clean state handling
- Responsive layout
- Polished user experience

### Database

- SQL Server
- EF Core Migrations
- Explicit indexes where useful
- Normalized relational model for core business entities
- JSON columns may be used for flexible external API data where appropriate

### AI

- AI integration must be implemented server-side only.
- API keys must never be exposed to the client.
- AI output must be validated before being used by the application.
- AI search should use structured criteria, not free-form untrusted results.
- The database remains the source of truth for Pokémon data.

---

## Project Structure

Recommended backend structure:

```text
pokemonTrainer/
│
├── Controllers/
│   ├── AuthController.cs
│   ├── HealthController.cs
│   ├── PokemonController.cs
│   ├── PokemonSyncController.cs
│   ├── DreamTeamController.cs
│   └── AiController.cs
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── DTOs/
│   ├── Auth/
│   ├── Pokemon/
│   ├── DreamTeam/
│   └── Ai/
│
├── Infrastructure/
│   ├── PokeApiClient.cs
│   └── AiProviderClient.cs
│
├── Models/
│   ├── ApplicationUser.cs
│   ├── Pokemon.cs
│   ├── PokemonType.cs
│   ├── PokemonPokemonType.cs
│   ├── DreamTeam.cs
│   └── DreamTeamSlot.cs
│
├── Services/
│   ├── JwtTokenService.cs
│   ├── PokemonSyncService.cs
│   ├── PokemonSearchService.cs
│   ├── DreamTeamService.cs
│   └── AiTrainerService.cs
│
├── Migrations/
│
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

If the application grows, the solution may later be split into separate projects such as API, Application, Domain, Infrastructure, and Tests. Do not introduce this split prematurely if it slows down delivery.

---

## Backend Guidelines

### Controllers

Controllers should remain thin.

Controllers are responsible for:

- Receiving HTTP requests.
- Validating request models.
- Calling application services.
- Returning appropriate HTTP responses.

Controllers should not contain:

- Heavy business logic.
- Direct calls to external APIs.
- Manual JWT generation logic.
- Complex database queries.
- UI-related behavior.

### Services

Business logic should be placed in services.

Examples:

- `JwtTokenService` creates JWT tokens.
- `PokemonSyncService` imports Pokémon data.
- `PokemonSearchService` performs filtering and search.
- `DreamTeamService` manages the user's team.
- `AiTrainerService` coordinates AI features.

### DTOs

Use DTOs for API input and output.

Do not expose EF entities directly unless there is a deliberate reason.

DTOs should be grouped by feature:

```text
DTOs/Auth
DTOs/Pokemon
DTOs/DreamTeam
DTOs/Ai
```

### Entity Framework

Use EF Core consistently:

- Keep `ApplicationDbContext` under `Data`.
- Define relationships explicitly in `OnModelCreating`.
- Add indexes for frequently searched fields.
- Use migrations with descriptive names.
- Avoid unnecessary database queries.
- Prefer async database operations.

Recommended migration names:

```text
InitialCreate
AddIdentityTables
AddPokemonTables
AddDreamTeamTables
AddAiLogs
```

---

## Authentication and Authorization

The system must require registration and login before accessing application data.

Use:

- ASP.NET Core Identity for user management.
- JWT Bearer Authentication for API authentication.
- `[Authorize]` for protected endpoints.

JWT claims should contain only basic identity information:

- User ID
- Email
- Display name
- Optional role claims when needed

Do not include sensitive information in JWT claims.

Never include:

- Passwords
- Password hashes
- API keys
- Connection strings
- Personal sensitive data

Authentication answers: "Who is the user?"

Authorization answers: "What is the user allowed to do?"

---

## Pokémon Data Flow

The client must not call PokeAPI directly.

Correct data flow:

```text
Angular Client
    ↓
ASP.NET Core Web API
    ↓
SQL Server
    ↑
Pokemon Sync Service
    ↑
PokeAPI
```

PokeAPI should be treated as an external source used for synchronization.

The local SQL Server database should be the primary runtime source for application reads.

This improves:

- Performance
- Reliability
- Search capabilities
- Error handling
- Control over the data model

---

## Pokémon Model Guidelines

Use a relational model for core Pokémon data.

Recommended core entities:

- `Pokemon`
- `PokemonType`
- `PokemonPokemonType`

Use a many-to-many relationship because a Pokémon can have multiple types, and a type can belong to many Pokémon.

Examples:

```text
Pikachu -> Electric
Charizard -> Fire + Flying
Bulbasaur -> Grass + Poison
```

Store flexible external API data such as stats and abilities as JSON if it helps delivery speed and keeps the model manageable.

Use indexes on:

- `PokeApiId`
- `Name`
- `PokemonType.Name`

---

## Dream Team Guidelines

Each authenticated user can manage a personal dream team.

Rules:

- A team can contain up to 5 Pokémon.
- The same Pokémon should not appear twice in the same team.
- Each selected Pokémon may have an optional nickname.
- Team data must be persisted in SQL Server.
- The user ID must be taken from the authenticated JWT context.
- Do not accept a user ID from the client for team ownership.

All ownership checks must be enforced server-side.

---

## AI Search Guidelines

AI-assisted search should be one of the strongest parts of the product.

Do not let the AI return arbitrary Pokémon names as the final source of truth.

Correct flow:

```text
User natural language request
    ↓
AI converts request into structured search criteria
    ↓
Server validates the criteria
    ↓
Server queries SQL Server
    ↓
Server returns real Pokémon records
    ↓
AI may generate a friendly explanation based on real data
```

Example structured search output:

```json
{
  "types": ["electric"],
  "excludeTypes": [],
  "minStats": {
    "speed": 90,
    "attack": 60
  },
  "excludeLegendary": true,
  "style": "aggressive",
  "limit": 12,
  "sortBy": "speed_desc"
}
```

If AI is unavailable:

- Fall back to regular search.
- Return a clear and friendly message.
- Do not fail the entire search experience.

---

## Team Analysis Guidelines

The team analyzer should first calculate facts on the server, then use AI only to explain them.

Server-side analysis may include:

- Team size
- Type coverage
- Duplicate types
- Average stats
- Missing roles
- Weakness warnings
- Suggested complementary types

AI should be used to turn this structured analysis into a clear and engaging explanation.

---

## UI and UX Guidelines

The UI should feel like a polished product, not a basic data grid.

Recommended UX elements:

- Modern dashboard
- Pokémon cards with official artwork
- Type badges with clear colors
- Stats bars
- Dream team builder with 5 visible slots
- Empty states
- Loading skeletons
- Toast notifications
- Responsive design
- Smooth but minimal animations
- Clear call-to-action buttons

Important screens:

- Login
- Register
- Dashboard
- Pokédex
- Pokémon details
- Dream team builder
- AI trainer/search panel

AI search should show what the AI understood before displaying results.

Example:

```text
Professor AI understood:
Type: Electric
Speed: 90+
Style: Aggressive
Exclude Legendary: Yes
```

This improves trust and makes the interaction more transparent.

---

## Error Handling Guidelines

Handle predictable failures gracefully:

- Database unavailable
- PokeAPI unavailable
- AI provider unavailable
- Invalid JWT
- Expired JWT
- Unauthorized access
- Attempt to add more than 5 Pokémon
- Attempt to add duplicate Pokémon
- Attempt to update another user's team

Do not expose stack traces to the client.

Return consistent error responses.

Use logs for diagnostics.

---

## Performance Guidelines

Performance decisions should be visible in the implementation.

Guidelines:

- Use server-side pagination.
- Do not load all Pokémon into the client at once.
- Use database indexes.
- Read Pokémon data from the local database.
- Avoid runtime dependency on PokeAPI for normal browsing.
- Consider caching stable lookup data such as Pokémon types.
- Use debounce on client-side search input.
- Use async HTTP and database calls.

---

## Security Guidelines

- Keep secrets out of source control.
- Store development secrets in User Secrets or environment variables when possible.
- Never expose AI keys to Angular.
- Validate all client input.
- Do not trust client-supplied user IDs.
- Protect private endpoints with `[Authorize]`.
- Use HTTPS locally and in production.
- Keep Swagger enabled only for development unless there is a specific reason otherwise.

---

## Code Quality Guidelines

Follow these practices:

- Use clear names.
- Keep methods focused.
- Prefer small services over large controllers.
- Avoid duplicated logic.
- Use DTOs for public API contracts.
- Use async/await for I/O.
- Keep formatting consistent.
- Add comments only when they clarify non-obvious decisions.
- Prefer readable code over clever code.

---

## Documentation Guidelines

Maintain documentation during development.

Recommended documentation:

```text
docs/
├── architecture.md
├── api.md
├── demo-script.md
└── ai-workflow/
    ├── STEP_01_BACKEND_FOUNDATION_AUTH_POKEMON_MODEL.md
    ├── STEP_02_POKEAPI_SYNC.md
    ├── STEP_03_POKEDEX_API.md
    ├── STEP_04_DREAM_TEAM.md
    └── STEP_05_AI_SEARCH_AND_ANALYSIS.md
```

Each step document should include:

- Goal
- Files created or modified
- Main decisions
- How it was tested
- Known limitations
- Next step

---

## AI Collaboration Notes

When AI is used to assist implementation, the result must still be reviewed, understood, and adjusted manually.

AI may be used for:

- Generating initial structure
- Suggesting DTOs
- Drafting service logic
- Improving UI ideas
- Producing documentation
- Finding edge cases

The developer remains responsible for:

- Architecture
- Final code decisions
- Security
- Correctness
- Testing
- Explaining the implementation

---

## Required Step Completion Format

At the end of every implementation step, provide this summary:

```text
Step summary:
- Goal:
- Files created:
- Files modified:
- How to test:
- Notes:
- Next recommended step:

Waiting for approval before continuing.
```

Do not continue without approval.
