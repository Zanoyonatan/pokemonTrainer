using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
using pokemonTrainer.DTOs.Common;
using pokemonTrainer.DTOs.Nicknames;
using pokemonTrainer.DTOs.Pokemon;
using pokemonTrainer.Models;
using pokemonTrainer.Services.Ai;

namespace pokemonTrainer.Services;

public class PokemonNicknameService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly PokemonCatalogCacheService _cacheService;
    private readonly GeminiTextGenerationService _geminiService;
    private readonly ILogger<PokemonNicknameService> _logger;

    private static readonly Dictionary<string, List<string>> FallbackNicknames = new()
    {
        { "electric", new() { "Sparky", "Volt", "Thunder", "Zappy", "Bolt" } },
        { "fire", new() { "Blaze", "Ember", "Flame", "Scorch", "Inferno" } },
        { "water", new() { "Splash", "Aqua", "Wave", "Bubbles", "Tide" } },
        { "grass", new() { "Leafy", "Sprout", "Vine", "Petal", "Bloom" } },
        { "psychic", new() { "Mystic", "Spirit", "Mind", "Prism", "Echo" } },
        { "ground", new() { "Quake", "Rocky", "Dust", "Terra", "Sand" } },
        { "flying", new() { "Sky", "Wind", "Soar", "Gust", "Breeze" } },
        { "dragon", new() { "Draco", "Scale", "Fury", "Tyrant", "Wyvern" } }
    };

    private static readonly List<string> DefaultNicknames = new()
    {
        "Buddy", "Champ", "Hero", "Scout", "Ace"
    };

    public PokemonNicknameService(
        ApplicationDbContext dbContext,
        PokemonCatalogCacheService cacheService,
        GeminiTextGenerationService geminiService,
        ILogger<PokemonNicknameService> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<ServiceResult<GeneratePokemonNicknamesResponse>> GenerateAsync(
        int pokeApiId,
        int count,
        CancellationToken cancellationToken = default)
    {
        // Try to load from database first
        try
        {
            var pokemon = await _dbContext.Pokemons
                .AsNoTracking()
                .Include(p => p.PokemonTypes)
                    .ThenInclude(pt => pt.PokemonType)
                .FirstOrDefaultAsync(
                    p => p.PokeApiId == pokeApiId,
                    cancellationToken);

            if (pokemon == null)
            {
                return ServiceResult<GeneratePokemonNicknamesResponse>.Fail(
                    "POKEMON_NOT_FOUND",
                    "Pokémon not found.");
            }

            return await GenerateForPokemonAsync(pokemon.Name, pokemon.PokemonTypes, count, cancellationToken);
        }
        catch (Exception ex) when (DatabaseAvailabilityService.IsDatabaseUnavailableException(ex))
        {
            // Fall back to cache
            var cached = _cacheService.FindInCache(pokeApiId);
            if (cached == null)
            {
                return ServiceResult<GeneratePokemonNicknamesResponse>.Fail(
                    "POKEMON_NOT_FOUND",
                    "Pokémon not found.");
            }

            return await GenerateForCacheItemAsync(cached, count, cancellationToken);
        }
    }

    private async Task<ServiceResult<GeneratePokemonNicknamesResponse>> GenerateForPokemonAsync(
        string pokemonName,
        ICollection<PokemonPokemonType> pokemonTypes,
        int count,
        CancellationToken cancellationToken)
    {
        count = Math.Clamp(count, 1, 10);

        var types = pokemonTypes
            .Select(pt => pt.PokemonType.Name.ToLower())
            .ToList();

        var response = new GeneratePokemonNicknamesResponse
        {
            PokeApiId = 0, // Will be set by caller
            PokemonName = pokemonName,
            Types = types
        };

        // Try Gemini first
        var aiSuggestions = await TryGenerateWithGeminiAsync(
            pokemonName,
            types,
            count,
            cancellationToken);

        if (aiSuggestions != null)
        {
            response.Suggestions = aiSuggestions;
            response.AiUsed = true;
        }
        else
        {
            // Fall back to deterministic suggestions
            response.Suggestions = GetFallbackSuggestions(types, count);
            response.AiUsed = false;
        }

        return ServiceResult<GeneratePokemonNicknamesResponse>.Ok(response);
    }

    private async Task<ServiceResult<GeneratePokemonNicknamesResponse>> GenerateForCacheItemAsync(
        PokemonCatalogCacheItem cached,
        int count,
        CancellationToken cancellationToken)
    {
        count = Math.Clamp(count, 1, 10);

        var types = cached.Types
            .Select(t => t.ToLower())
            .ToList();

        var response = new GeneratePokemonNicknamesResponse
        {
            PokeApiId = cached.PokeApiId,
            PokemonName = cached.Name,
            Types = types
        };

        // Try Gemini first
        var aiSuggestions = await TryGenerateWithGeminiAsync(
            cached.Name,
            types,
            count,
            cancellationToken);

        if (aiSuggestions != null)
        {
            response.Suggestions = aiSuggestions;
            response.AiUsed = true;
        }
        else
        {
            // Fall back to deterministic suggestions
            response.Suggestions = GetFallbackSuggestions(types, count);
            response.AiUsed = false;
        }

        return ServiceResult<GeneratePokemonNicknamesResponse>.Ok(response);
    }

    private async Task<List<string>?> TryGenerateWithGeminiAsync(
        string pokemonName,
        List<string> types,
        int count,
        CancellationToken cancellationToken)
    {
        try
        {
            var prompt = BuildGeminiPrompt(pokemonName, types, count);
            var jsonResponse = await _geminiService.GenerateJsonAsync(prompt, cancellationToken);

            if (string.IsNullOrEmpty(jsonResponse))
            {
                return null;
            }

            var suggestions = ParseGeminiResponse(jsonResponse, count);
            if (suggestions?.Count > 0)
            {
                return suggestions;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to generate nicknames with Gemini for Pokémon {PokemonName}.",
                pokemonName);
        }

        return null;
    }

    private string BuildGeminiPrompt(string pokemonName, List<string> types, int count)
    {
        var typesList = types.Count > 0 ? string.Join(", ", types) : "normal";

        return $@"Generate exactly {count} short, fun, and family-friendly nickname suggestions for a Pokémon named ""{pokemonName}"" with types: {typesList}.

Requirements:
- Return ONLY a JSON array of strings
- Each nickname should be 1-2 words max
- Names should be creative but not silly
- No explanations or additional text
- Return exactly {count} unique suggestions

Format:
{{
  ""suggestions"": [""name1"", ""name2"", ...]
}}";
    }

    private List<string>? ParseGeminiResponse(string jsonResponse, int expectedCount)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            if (root.TryGetProperty("suggestions", out var suggestionsElement) &&
                suggestionsElement.ValueKind == JsonValueKind.Array)
            {
                var suggestions = new List<string>();

                foreach (var item in suggestionsElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var suggestion = item.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(suggestion))
                        {
                            suggestions.Add(suggestion);
                        }
                    }
                }

                if (suggestions.Count == expectedCount)
                {
                    return suggestions;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini response for nicknames.");
        }

        return null;
    }

    private List<string> GetFallbackSuggestions(List<string> types, int count)
    {
        var suggestions = new List<string>();

        // Try to use type-specific nicknames first
        foreach (var type in types)
        {
            if (FallbackNicknames.TryGetValue(type, out var typeNicknames))
            {
                suggestions.AddRange(typeNicknames);
            }
        }

        // If we don't have enough type-specific names, add defaults
        if (suggestions.Count == 0)
        {
            suggestions.AddRange(DefaultNicknames);
        }

        // Deduplicate and shuffle to add variety
        suggestions = suggestions
            .Distinct()
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToList();

        // If still not enough, pad with defaults
        while (suggestions.Count < count)
        {
            foreach (var name in DefaultNicknames)
            {
                if (!suggestions.Contains(name))
                {
                    suggestions.Add(name);
                    if (suggestions.Count >= count)
                    {
                        break;
                    }
                }
            }
        }

        return suggestions.Take(count).ToList();
    }
}
