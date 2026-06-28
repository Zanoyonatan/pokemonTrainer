using System.Text.Json;
using pokemonTrainer.DTOs.AiSearch;
using pokemonTrainer.Services.Ai;

namespace pokemonTrainer.Services;

public class PokemonSmartSearchService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> KnownTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "normal",
        "fire",
        "water",
        "electric",
        "grass",
        "ice",
        "fighting",
        "poison",
        "ground",
        "flying",
        "psychic",
        "bug",
        "rock",
        "ghost",
        "dragon",
        "dark",
        "steel",
        "fairy"
    };

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "pokemon",
        "pokemons",
        "find",
        "show",
        "give",
        "me",
        "with",
        "for",
        "and",
        "of",
        "the",
        "a",
        "an"
    };

    private static readonly HashSet<string> IntentWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "small",
        "short",
        "tiny",
        "large",
        "big",
        "tall",
        "light",
        "heavy",
        "fast",
        "speed",
        "quick",
        "strong",
        "attack",
        "attacker",
        "defensive",
        "defense",
        "tank",
        "hp",
        "health",
        "healthy",
        "special",
        "experienced",
        "experience",
        "xp"
    };

    private readonly PokemonService _pokemonService;
    private readonly GeminiTextGenerationService _geminiService;
    private readonly ILogger<PokemonSmartSearchService> _logger;

    public PokemonSmartSearchService(
        PokemonService pokemonService,
        GeminiTextGenerationService geminiService,
        ILogger<PokemonSmartSearchService> logger)
    {
        _pokemonService = pokemonService;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<PokemonSmartSearchResponse> SearchAsync(
        PokemonSmartSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var criteria =
            await TryParseCriteriaWithGeminiAsync(
                request.Query,
                cancellationToken)
            ?? ParseCriteriaByRules(request.Query);

        var results = await _pokemonService.SearchByCriteriaAsync(
            criteria,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PokemonSmartSearchResponse
        {
            Criteria = criteria,
            Results = results,
            Explanation = BuildExplanation(criteria, results.TotalCount)
        };
    }

    private async Task<PokemonSmartSearchCriteria?> TryParseCriteriaWithGeminiAsync(
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var prompt = BuildGeminiPrompt(query);

            var json = await _geminiService.GenerateJsonAsync(
                prompt,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var cleanedJson = CleanJson(json);

            var criteria =
                JsonSerializer.Deserialize<PokemonSmartSearchCriteria>(
                    cleanedJson,
                    JsonOptions);

            if (criteria == null)
            {
                return null;
            }

            SanitizeCriteria(criteria, query);

            criteria.DetectedIntents.Add("parser:gemini");

            return criteria;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Gemini criteria parsing failed. Falling back to rule-based parser.");

            return null;
        }
    }

    private static string BuildGeminiPrompt(string query)
    {
        return $$"""
You are a Pokémon search parser.

Your job is to convert a user's natural language search query into a strict JSON object.

Do not return explanations.
Do not return markdown.
Return only valid JSON.

The application will use this JSON to query SQL Server.
Do not invent Pokémon.
Do not return Pokémon names as results.
Only return search criteria.

Allowed Pokémon types:
normal, fire, water, electric, grass, ice, fighting, poison, ground, flying, psychic, bug, rock, ghost, dragon, dark, steel, fairy

Allowed sortBy values:
hp, attack, defense, specialAttack, specialDefense, speed, height, weight, baseExperience, null

sortDirection must be:
asc or desc

Use these interpretations:
- fast, quick, speed => sortBy speed desc
- strong, attacker, attack => sortBy attack desc
- defensive, tank => sortBy defense desc
- high hp, health => sortBy hp desc
- special attacker => sortBy specialAttack desc
- special defense => sortBy specialDefense desc
- experienced, xp => sortBy baseExperience desc
- small, short, tiny => maxHeight 10
- large, big, tall => minHeight 20
- light => maxWeight 200
- heavy => minWeight 1000
- if the query contains a known Pokémon type, set type
- if the query looks like a Pokémon name, set nameSearch

Schema:
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

User query:
{{JsonSerializer.Serialize(query)}}
""";
    }

    private static string CleanJson(string value)
    {
        var cleaned = value.Trim();

        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[7..].Trim();
        }

        if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[3..].Trim();
        }

        if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[..^3].Trim();
        }

        return cleaned;
    }

    private static void SanitizeCriteria(
        PokemonSmartSearchCriteria criteria,
        string originalQuery)
    {
        criteria.OriginalQuery = originalQuery;

        if (!string.IsNullOrWhiteSpace(criteria.Type) &&
            !KnownTypes.Contains(criteria.Type))
        {
            criteria.Type = null;
        }

        var allowedSortBy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hp",
            "attack",
            "defense",
            "specialAttack",
            "specialDefense",
            "speed",
            "height",
            "weight",
            "baseExperience"
        };

        if (!string.IsNullOrWhiteSpace(criteria.SortBy) &&
            !allowedSortBy.Contains(criteria.SortBy))
        {
            criteria.SortBy = null;
        }

        if (!criteria.SortDirection.Equals(
                "desc",
                StringComparison.OrdinalIgnoreCase))
        {
            criteria.SortDirection = "asc";
        }
        else
        {
            criteria.SortDirection = "desc";
        }

        criteria.DetectedIntents ??= new List<string>();
    }

    private static PokemonSmartSearchCriteria ParseCriteriaByRules(string query)
    {
        var normalizedQuery = query
            .Trim()
            .ToLowerInvariant();

        var words = normalizedQuery
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var criteria = new PokemonSmartSearchCriteria
        {
            OriginalQuery = query
        };

        DetectType(words, criteria);
        DetectHeightIntent(normalizedQuery, criteria);
        DetectWeightIntent(normalizedQuery, criteria);
        DetectStatIntent(normalizedQuery, criteria);
        DetectNameSearch(words, criteria);

        if (criteria.DetectedIntents.Count == 0)
        {
            criteria.NameSearch = normalizedQuery;
            criteria.DetectedIntents.Add("search:name");
        }

        criteria.DetectedIntents.Add("parser:rules");

        return criteria;
    }

    private static void DetectType(
        List<string> words,
        PokemonSmartSearchCriteria criteria)
    {
        var detectedType = words.FirstOrDefault(word =>
            KnownTypes.Contains(word));

        if (string.IsNullOrWhiteSpace(detectedType))
        {
            return;
        }

        criteria.Type = detectedType;
        criteria.DetectedIntents.Add($"type:{detectedType}");
    }

    private static void DetectHeightIntent(
        string normalizedQuery,
        PokemonSmartSearchCriteria criteria)
    {
        if (ContainsAny(normalizedQuery, "small", "short", "tiny"))
        {
            criteria.MaxHeight = 10;
            criteria.DetectedIntents.Add("height:small");
        }

        if (ContainsAny(normalizedQuery, "large", "big", "tall"))
        {
            criteria.MinHeight = 20;
            criteria.DetectedIntents.Add("height:large");
        }
    }

    private static void DetectWeightIntent(
        string normalizedQuery,
        PokemonSmartSearchCriteria criteria)
    {
        if (ContainsAny(normalizedQuery, "light"))
        {
            criteria.MaxWeight = 200;
            criteria.DetectedIntents.Add("weight:light");
        }

        if (ContainsAny(normalizedQuery, "heavy"))
        {
            criteria.MinWeight = 1000;
            criteria.DetectedIntents.Add("weight:heavy");
        }
    }

    private static void DetectStatIntent(
        string normalizedQuery,
        PokemonSmartSearchCriteria criteria)
    {
        if (ContainsAny(normalizedQuery, "fast", "speed", "quick"))
        {
            criteria.SortBy = "speed";
            criteria.SortDirection = "desc";
            criteria.DetectedIntents.Add("sort:speed");
            return;
        }

        if (ContainsAny(normalizedQuery, "strong", "attack", "attacker"))
        {
            criteria.SortBy = "attack";
            criteria.SortDirection = "desc";
            criteria.DetectedIntents.Add("sort:attack");
            return;
        }

        if (ContainsAny(normalizedQuery, "defensive", "defense", "tank"))
        {
            criteria.SortBy = "defense";
            criteria.SortDirection = "desc";
            criteria.DetectedIntents.Add("sort:defense");
            return;
        }

        if (ContainsAny(normalizedQuery, "hp", "health", "healthy"))
        {
            criteria.SortBy = "hp";
            criteria.SortDirection = "desc";
            criteria.DetectedIntents.Add("sort:hp");
            return;
        }

        if (ContainsAny(normalizedQuery, "special attack", "special attacker"))
        {
            criteria.SortBy = "specialAttack";
            criteria.SortDirection = "desc";
            criteria.DetectedIntents.Add("sort:specialAttack");
            return;
        }

        if (ContainsAny(normalizedQuery, "special defense", "special defensive"))
        {
            criteria.SortBy = "specialDefense";
            criteria.SortDirection = "desc";
            criteria.DetectedIntents.Add("sort:specialDefense");
            return;
        }

        if (ContainsAny(normalizedQuery, "experienced", "experience", "xp"))
        {
            criteria.SortBy = "baseExperience";
            criteria.SortDirection = "desc";
            criteria.DetectedIntents.Add("sort:baseExperience");
        }
    }

    private static void DetectNameSearch(
        List<string> words,
        PokemonSmartSearchCriteria criteria)
    {
        var possibleNameWords = words
            .Where(word => !KnownTypes.Contains(word))
            .Where(word => !StopWords.Contains(word))
            .Where(word => !IntentWords.Contains(word))
            .ToList();

        if (possibleNameWords.Count == 0)
        {
            return;
        }

        criteria.NameSearch = string.Join(" ", possibleNameWords);
        criteria.DetectedIntents.Add($"search:name:{criteria.NameSearch}");
    }

    private static bool ContainsAny(
        string value,
        params string[] options)
    {
        return options.Any(option =>
            value.Contains(option, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildExplanation(
        PokemonSmartSearchCriteria criteria,
        int totalCount)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(criteria.Type))
        {
            parts.Add($"type '{criteria.Type}'");
        }

        if (!string.IsNullOrWhiteSpace(criteria.NameSearch))
        {
            parts.Add($"name search '{criteria.NameSearch}'");
        }

        if (criteria.MinHeight.HasValue || criteria.MaxHeight.HasValue)
        {
            parts.Add("height preference");
        }

        if (criteria.MinWeight.HasValue || criteria.MaxWeight.HasValue)
        {
            parts.Add("weight preference");
        }

        if (!string.IsNullOrWhiteSpace(criteria.SortBy))
        {
            parts.Add($"sorted by '{criteria.SortBy}'");
        }

        var interpretedAs = parts.Count == 0
            ? "a general Pokémon search"
            : string.Join(", ", parts);

        return $"The query was interpreted as {interpretedAs}. Found {totalCount} matching Pokémon.";
    }
}