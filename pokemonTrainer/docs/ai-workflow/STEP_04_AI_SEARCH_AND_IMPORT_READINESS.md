Implement two related backend improvements for the Pokémon Trainer ASP.NET Core Web API:

1. AI-powered Smart Search using Gemini with a deterministic fallback parser.
2. Robust Pokémon catalog import/readiness handling using a persisted catalog state table instead of hard-coded thresholds.

The system must remain reliable even if Gemini, PokeAPI, or the database has temporary issues.

============================================================
PART 1 — AI SMART SEARCH WITH GEMINI + FALLBACK
===============================================

Goal:
Allow users to search Pokémon using natural language queries such as:

* fast electric pokemon
* strong fire pokemon
* high hp water pokemon
* defensive rock pokemon
* small electric pokemon
* heavy dragon pokemon
* experienced fire pokemon
* pikachu

Important:
Gemini must not return Pokémon results directly.
Gemini should only convert the user query into structured criteria.
The actual search must always run against SQL Server using the local Pokémon database.

Architecture:
Natural language query
→ Gemini parser
→ structured search criteria
→ SQL Server query
→ paged Pokémon results

If Gemini is unavailable, disabled, missing an API key, times out, or returns invalid JSON:
fallback to the existing rule-based parser.

The AI must add value, but it must not become a single point of failure.

---

1. Create AI Search DTOs

---

Create folder:
DTOs/AiSearch

Create:
DTOs/AiSearch/PokemonSmartSearchRequest.cs

Properties:

* string Query

  * Required
  * MinLength(2)
  * MaxLength(300)
* int Page = 1
* int PageSize = 20

Create:
DTOs/AiSearch/PokemonSmartSearchCriteria.cs

Properties:

* string OriginalQuery
* string? Type
* string? NameSearch
* string? SortBy
* string SortDirection = "asc"
* int? MinHeight
* int? MaxHeight
* int? MinWeight
* int? MaxWeight
* List<string> DetectedIntents = new()

Allowed SortBy values:

* hp
* attack
* defense
* specialAttack
* specialDefense
* speed
* height
* weight
* baseExperience

Create:
DTOs/AiSearch/PokemonSmartSearchResponse.cs

Properties:

* PokemonSmartSearchCriteria Criteria
* PokemonPagedResponse Results
* string Explanation

---

2. Ensure Pokémon stat columns exist

---

The Pokemon entity should have searchable stat columns:

* Hp
* Attack
* Defense
* SpecialAttack
* SpecialDefense
* Speed

Keep StatsJson for details display, but use the stat columns for sorting and smart search.

If these columns do not exist yet:

* Add them to Models/Pokemon.cs
* Update PokemonImportService so imported Pokémon populate these fields from PokeAPI stats:

  * hp
  * attack
  * defense
  * special-attack
  * special-defense
  * speed
* Add a migration.
* Backfill existing Pokémon rows from StatsJson into the new columns.

---

3. Update PokemonService

---

Add method:

SearchByCriteriaAsync(
PokemonSmartSearchCriteria criteria,
int page = 1,
int pageSize = 20,
CancellationToken cancellationToken = default)

Behavior:

* Clamp page to minimum 1
* Clamp pageSize between 1 and 100
* Start from _dbContext.Pokemons.AsNoTracking()
* Apply name search if criteria.NameSearch exists
* Apply type filter if criteria.Type exists
* Apply height filters:

  * MinHeight
  * MaxHeight
* Apply weight filters:

  * MinWeight
  * MaxWeight
* Apply sorting based on criteria.SortBy and criteria.SortDirection
* Return PokemonPagedResponse

ApplySorting must support:

* hp
* attack
* defense
* specialattack
* specialdefense
* speed
* height
* weight
* baseexperience

Example mappings:

* "speed" desc → OrderByDescending(p => p.Speed)
* "attack" desc → OrderByDescending(p => p.Attack)
* "defense" desc → OrderByDescending(p => p.Defense)
* "hp" desc → OrderByDescending(p => p.Hp)
* "baseexperience" desc → OrderByDescending(p => p.BaseExperience)

Default sorting:
OrderBy(p => p.PokeApiId)

---

4. Add Gemini configuration

---

Create:
Options/GeminiOptions.cs

Properties:

* bool Enabled
* string ApiKey
* string Model

Do not hard-code the API key.
The model must come from configuration.

Example:

public class GeminiOptions
{
public bool Enabled { get; set; }
public string ApiKey { get; set; } = string.Empty;
public string Model { get; set; } = "configure-model-name-here";
}

Update appsettings.Development.json:

"Gemini": {
"Enabled": true,
"Model": "configure-model-name-here"
}

Do not store the API key in appsettings.json.
Use User Secrets for local development:

{
"Gemini": {
"ApiKey": "PASTE_GEMINI_API_KEY_HERE"
}
}

---

5. Add GeminiTextGenerationService

---

Create folder:
Services/Ai

Create:
Services/Ai/GeminiTextGenerationService.cs

Responsibilities:

* Inject HttpClient
* Inject IOptions<GeminiOptions>
* Inject ILogger<GeminiTextGenerationService>

Expose method:

Task<string?> GenerateJsonAsync(
string prompt,
CancellationToken cancellationToken = default)

Behavior:

* If Gemini is disabled or ApiKey is missing, return null.
* Build request body with:

  * contents
  * parts
  * text prompt
  * generationConfig
  * temperature = 0.1
  * responseMimeType = "application/json"
* Send POST request to:
  models/{Model}:generateContent
* Add header:
  x-goog-api-key: ApiKey
* If response is not successful:

  * log warning
  * return null
* Parse Gemini response:
  candidates[0].content.parts[0].text
* Return the extracted text.
* Do not throw to the controller on Gemini failure.

---

6. Update Program.cs for Gemini

---

Add using statements:

* pokemonTrainer.Options
* pokemonTrainer.Services.Ai

Register Gemini options:

builder.Services.Configure<GeminiOptions>(
builder.Configuration.GetSection("Gemini"));

Register Gemini HttpClient:

builder.Services.AddHttpClient<GeminiTextGenerationService>(client =>
{
client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
client.Timeout = TimeSpan.FromSeconds(20);
});

Register Smart Search service:

builder.Services.AddScoped<PokemonSmartSearchService>();

---

7. Implement PokemonSmartSearchService

---

File:
Services/PokemonSmartSearchService.cs

Inject:

* PokemonService
* GeminiTextGenerationService
* ILogger<PokemonSmartSearchService>

Public method:

Task<PokemonSmartSearchResponse> SearchAsync(
PokemonSmartSearchRequest request,
CancellationToken cancellationToken = default)

Flow:

1. Try parsing criteria with Gemini.
2. If Gemini returns valid criteria, use it.
3. If Gemini fails, returns null, invalid JSON, invalid type, or invalid sortBy:
   fallback to rule-based parser.
4. Use PokemonService.SearchByCriteriaAsync to query SQL Server.
5. Return:

   * Criteria
   * Results
   * Explanation

Gemini parser:

* Build a strict prompt that asks Gemini to return JSON only.
* No markdown.
* No explanations.
* No Pokémon results.
* Only structured criteria.

The prompt should include:

Allowed Pokémon types:
normal, fire, water, electric, grass, ice, fighting, poison, ground, flying, psychic, bug, rock, ghost, dragon, dark, steel, fairy

Allowed sortBy values:
hp, attack, defense, specialAttack, specialDefense, speed, height, weight, baseExperience, null

Intent rules:

* fast / quick / speed → sortBy speed desc
* strong / attack / attacker → sortBy attack desc
* defensive / tank → sortBy defense desc
* high hp / health → sortBy hp desc
* special attacker → sortBy specialAttack desc
* special defense → sortBy specialDefense desc
* experienced / xp → sortBy baseExperience desc
* small / short / tiny → maxHeight 10
* large / big / tall → minHeight 20
* light → maxWeight 200
* heavy → minWeight 1000
* known Pokémon type → set Type
* Pokémon name-like query → set NameSearch

Expected JSON schema:
{
"originalQuery": "string",
"type": "string or null",
"nameSearch": "string or null",
"sortBy": "string or null",
"sortDirection": "asc or desc",
"minHeight": number or null,
"maxHeight": number or null,
"minWeight": number or null,
"maxWeight": number or null,
"detectedIntents": ["string"]
}

After Gemini returns JSON:

* Clean possible markdown fences such as ```json
* Deserialize into PokemonSmartSearchCriteria
* Sanitize:

  * If Type is not one of the known Pokémon types, set it to null.
  * If SortBy is not allowed, set it to null.
  * If SortDirection is not desc, normalize to asc.
  * Ensure DetectedIntents is not null.
  * Set OriginalQuery to the original user query.
  * Add detected intent: parser:gemini.

Rule-based fallback parser:
Implement deterministic parsing for:

* known Pokémon types
* size intents
* weight intents
* stat intents
* name search

Known types:
normal, fire, water, electric, grass, ice, fighting, poison, ground, flying, psychic, bug, rock, ghost, dragon, dark, steel, fairy

Stop words:
pokemon, pokemons, find, show, give, me, with, for, and, of, the, a, an

Intent words:
small, short, tiny, large, big, tall, light, heavy,
fast, speed, quick,
strong, attack, attacker,
defensive, defense, tank,
hp, health, healthy,
special,
experienced, experience, xp

Rule examples:

* "fast electric pokemon"

  * Type = electric
  * SortBy = speed
  * SortDirection = desc
  * DetectedIntents includes type:electric, sort:speed, parser:rules

* "strong fire pokemon"

  * Type = fire
  * SortBy = attack
  * SortDirection = desc

* "high hp water pokemon"

  * Type = water
  * SortBy = hp
  * SortDirection = desc

* "small electric pokemon"

  * Type = electric
  * MaxHeight = 10

* "pikachu"

  * NameSearch = pikachu

Build explanation:
Return a simple explanation such as:
"The query was interpreted as type 'electric', sorted by 'speed'. Found 10 matching Pokémon."

---

8. Add PokemonSmartSearchController

---

Create:
Controllers/PokemonSmartSearchController.cs

Controller requirements:

* [ApiController]
* [Authorize]
* [Route("api/pokemon-smart-search")]

Inject:

* PokemonSmartSearchService
* PokemonImportStatusService

Endpoint:
POST /api/pokemon-smart-search

Behavior:

* If PokemonImportStatusService.IsReady is false:
  return 503 with:
  {
  "status": "ImportInProgress",
  "message": "Pokémon data is still loading. Please try again shortly."
  }

* Otherwise call SmartSearchService.SearchAsync.

* Return Ok(response).

Security:

* Do not expose Gemini API key to Angular.
* Do not call Gemini from the client.
* Angular calls only the ASP.NET Core backend.
* Backend calls Gemini.
* Do not commit API key to Git.
* Use User Secrets for local development.

============================================================
PART 2 — ROBUST POKÉMON IMPORT READINESS
========================================

Goal:
Do not use hard-coded thresholds such as 1000 or 1300 to decide whether the Pokémon catalog is usable.
Instead, persist the last successful catalog import state in SQL Server and use it to decide whether existing local Pokémon data is usable when PokeAPI is unavailable or startup import is still running.

Core idea:

* If this is the first import and no complete catalog state exists:
  IsReady = false until a full import completes.
* If a previous full import succeeded:
  IsReady can remain true even if PokeAPI is currently unavailable, because the local SQL Server snapshot is usable.
* Never expose a partially imported initial catalog.
* Do not depend on PokeAPI being available at runtime for normal search usage.

---

9. Add PokemonCatalogState model

---

Create:
Models/PokemonCatalogState.cs

Properties:

* int Id
* int LastKnownRemoteCount
* int LocalCountAtLastSuccessfulImport
* bool IsComplete
* DateTime? LastSuccessfulImportAtUtc
* DateTime LastUpdatedAtUtc = DateTime.UtcNow

Important:

* Do not use a DefaultId constant.
* Do not manually assign Id = 1.
* Let SQL Server generate the identity value.

---

10. Update ApplicationDbContext

---

File:
Data/ApplicationDbContext.cs

Add:

public DbSet<PokemonCatalogState> PokemonCatalogStates { get; set; }

Inside OnModelCreating add:

builder.Entity<PokemonCatalogState>()
.HasKey(s => s.Id);

---

11. Update PokemonImportResult

---

File:
DTOs/PokemonImport/PokemonImportResult.cs

Add:

* int LocalCountAfter
* bool IsComplete

The result should contain:

* RemoteCount
* LocalCountBefore
* LocalCountAfter
* Checked
* Missing
* Created
* Skipped
* Failed
* IsComplete
* Errors

---

12. Update PokemonImportStatusResponse

---

File:
DTOs/PokemonImport/PokemonImportStatusResponse.cs

Add:

* int LocalCountAfter
* bool IsComplete

The status response should expose:

* whether the local catalog is complete
* the local count before import
* the local count after import
* the remote count from PokeAPI

---

13. Update PokemonImportStatusService

---

File:
Services/PokemonImportStatusService.cs

Support these statuses:

* NotStarted
* Running
* Completed
* CompletedWithWarnings
* Failed

Implement:

public void MarkRunning(bool keepExistingDataAvailable = false)

Behavior:

* If keepExistingDataAvailable is true, IsReady should be true.
* Message should say that import is running in the background and existing local data is available.
* If false, IsReady should be false.

Implement:

public void MarkCompleted(
PokemonImportResult result,
bool keepExistingDataAvailable = false)

Behavior:

* If result.IsComplete is true:

  * Status = Completed
  * IsReady = true
* If result.IsComplete is false but keepExistingDataAvailable is true:

  * Status = CompletedWithWarnings
  * IsReady = true
* Copy all result fields into the status response:

  * RemoteCount
  * LocalCountBefore
  * LocalCountAfter
  * Checked
  * Missing
  * Created
  * Skipped
  * Failed
  * IsComplete
  * Errors

Implement:

public void MarkFailed(
string message,
List<string>? errors = null,
bool keepExistingDataAvailable = false)

Behavior:

* If keepExistingDataAvailable is true:

  * IsReady = true even though Status = Failed
  * This means the external import failed, but the local snapshot is still usable.
* If false:

  * IsReady = false

---

14. Update PokemonImportService

---

File:
Services/PokemonImportService.cs

Add private helper method:

private async Task UpdateCatalogStateIfCompleteAsync(
PokemonImportResult result,
CancellationToken cancellationToken)

Behavior:

* If result.IsComplete is false, do nothing.
* Read the first existing PokemonCatalogState record:
  _dbContext.PokemonCatalogStates.FirstOrDefaultAsync(cancellationToken)
* If no state exists, create a new PokemonCatalogState.
* Do not manually set the Id.
* Update:

  * LastKnownRemoteCount = result.RemoteCount
  * LocalCountAtLastSuccessfulImport = result.LocalCountAfter
  * IsComplete = true
  * LastSuccessfulImportAtUtc = DateTime.UtcNow
  * LastUpdatedAtUtc = DateTime.UtcNow
* Save changes.

Update ImportMissingAsync:

A. After reading remoteCount and localCountBefore:
If maxCount is null and localCountBefore == remoteCount:

* Set Checked = 0
* Missing = 0
* Created = 0
* Failed = 0
* Skipped = remoteCount
* LocalCountAfter = localCountBefore
* IsComplete = true
* Call UpdateCatalogStateIfCompleteAsync
* Return result

This is important because even when there is nothing to import, the system must persist the successful catalog state.

B. If missingReferences.Count == 0:

* Set LocalCountAfter from DB count
* Set IsComplete =
  maxCount is null &&
  Failed == 0 &&
  LocalCountAfter >= RemoteCount
* Call UpdateCatalogStateIfCompleteAsync
* Return result

C. At the end of a real import:

* Save changes
* Set LocalCountAfter from DB count
* Set IsComplete =
  maxCount is null &&
  Failed == 0 &&
  LocalCountAfter >= RemoteCount
* Call UpdateCatalogStateIfCompleteAsync
* Return result

---

15. Update PokemonStartupImportWorker

---

File:
Workers/PokemonStartupImportWorker.cs

At startup, before calling ImportMissingAsync, check if there is a usable local catalog.

Implement:

private static async Task<bool> HasUsableLocalCatalogAsync(
ApplicationDbContext dbContext,
CancellationToken cancellationToken)

Logic:

* Count local Pokémon records.
* Read the latest PokemonCatalogState record:
  OrderByDescending(s => s.LastSuccessfulImportAtUtc)
  FirstOrDefaultAsync()
* Return true only if:
  catalogState != null &&
  catalogState.IsComplete &&
  localPokemonCount >= catalogState.LastKnownRemoteCount

Do not compare against a hard-coded number.

In ExecuteAsync:

* Call HasUsableLocalCatalogAsync.
* Call:
  _statusService.MarkRunning(keepExistingDataAvailable: hasUsableLocalCatalog)

Then run ImportMissingAsync.

On successful import:

* Call:
  _statusService.MarkCompleted(result, keepExistingDataAvailable: hasUsableLocalCatalog)

On timeout, cancellation, or exception:

* Call MarkFailed with keepExistingDataAvailable: hasUsableLocalCatalog.

Add helper method:

private static List<string> BuildErrors(Exception ex)

Behavior:

* Include ex.Message
* Include all inner exception messages recursively.
* Use BuildErrors in all exception handlers so SQL inner exceptions are visible in the status endpoint.

Important:

* Do not use PokemonCatalogState.DefaultId.
* Do not manually insert Id.
* Read the catalog state by latest successful import date.

---

16. Add migration

---

Run:

Add-Migration AddPokemonCatalogState
Update-Database

If the migration was already created with a DefaultId-based model, adjust the model first so Id is a normal identity column generated by SQL Server.

---

17. Validation

---

After running the application once successfully, verify:

SQL:
SELECT * FROM PokemonCatalogStates;

Expected:

* One row exists
* LastKnownRemoteCount equals the latest remote count from PokeAPI
* LocalCountAtLastSuccessfulImport equals the local Pokémon count
* IsComplete = true

API:
GET /api/pokemon-import-status

Expected if import completed:

* status = Completed
* isReady = true
* isComplete = true
* remoteCount > 0
* localCountAfter >= remoteCount

Expected if PokeAPI fails but a previous complete catalog exists:

* status = Failed
* isReady = true
* lastMessage explains that local Pokémon data is still available

Expected on first startup with no complete catalog:

* status = Running or Failed
* isReady = false
* Pokémon endpoints should return 503 until import completes.

============================================================
PART 3 — END-TO-END TESTING
===========================

1. Test fallback first:
   Set Gemini.Enabled = false

POST /api/pokemon-smart-search

Body:
{
"query": "fast electric pokemon",
"page": 1,
"pageSize": 5
}

Expected:

* criteria.type = electric
* criteria.sortBy = speed
* criteria.sortDirection = desc
* detectedIntents contains parser:rules
* results.items count <= pageSize
* items are sorted by speed descending

2. Test Gemini:
   Set Gemini.Enabled = true.
   Add Gemini API key to User Secrets.

Run the same request.

Expected:

* detectedIntents contains parser:gemini if Gemini worked.
* If Gemini fails, detectedIntents contains parser:rules.
* In both cases, results should still return from SQL Server.

3. Additional test queries:
   {
   "query": "strong fire pokemon",
   "page": 1,
   "pageSize": 5
   }

Expected:
type = fire
sortBy = attack

{
"query": "high hp water pokemon",
"page": 1,
"pageSize": 5
}

Expected:
type = water
sortBy = hp

{
"query": "small electric pokemon",
"page": 1,
"pageSize": 5
}

Expected:
type = electric
maxHeight = 10

{
"query": "pikachu",
"page": 1,
"pageSize": 5
}

Expected:
nameSearch = pikachu

============================================================
DESIGN EXPLANATION
==================

The AI integration is intentionally limited to criteria parsing.
Gemini does not return Pokémon results and does not replace the database.
SQL Server remains the source of truth.

The import readiness logic does not use arbitrary hard-coded counts.
The backend stores the last successful catalog state in SQL Server and uses that state to decide whether local Pokémon data is usable.

This gives the user:

* Natural-language search
* Fast SQL-based results
* AI-powered interpretation
* Safe fallback when Gemini is unavailable
* Safe fallback when PokeAPI is unavailable
* No exposure of a partially imported initial catalog
