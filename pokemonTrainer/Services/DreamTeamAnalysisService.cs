using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
using pokemonTrainer.DTOs.DreamTeamAnalysis;
using pokemonTrainer.Services.Ai;
using pokemonTrainer.DTOs.Pokemon;

namespace pokemonTrainer.Services;

public class DreamTeamAnalysisService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GeminiTextGenerationService _geminiService;
    private readonly ILogger<DreamTeamAnalysisService> _logger;
    private readonly PokemonCatalogAverageService _catalogAverageService;
    private const int maxTeamSize = 5;
    private static readonly List<string> RecommendedTypes = new()
    {
        "fire",
        "water",
        "electric",
        "grass",
        "ground",
        "flying",
        "psychic",
        "dragon"
    };

    public DreamTeamAnalysisService(
        ApplicationDbContext dbContext,
        GeminiTextGenerationService geminiService,
        PokemonCatalogAverageService catalogAverageService,
        ILogger<DreamTeamAnalysisService> logger)
    {
        _dbContext = dbContext;
        _geminiService = geminiService;
        _catalogAverageService = catalogAverageService;
        _logger = logger;
    }

    public async Task<DreamTeamAnalysisResponse> AnalyzeAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var teamPokemons = await _dbContext.DreamTeamPokemons
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Include(d => d.Pokemon)
                .ThenInclude(p => p.PokemonTypes)
                    .ThenInclude(pt => pt.PokemonType)
            .OrderBy(d => d.Slot)
            .ToListAsync(cancellationToken);

        var response = new DreamTeamAnalysisResponse
        {
            MaxTeamSize = 5,
            CurrentTeamSize = teamPokemons.Count,
            IsFullTeam = teamPokemons.Count == 5
        };

        if (teamPokemons.Count == 0)
        {
            response.TeamScore = 0;
            response.Summary = "Your Dream Team is empty. Start by adding Pokémon to build your team!";
            response.Recommendations.Add("Add your first Pokémon to get started.");
            response.AiSummaryUsed = false;

            return response;
        }

        var avgHp = teamPokemons.Average(t => t.Pokemon.Hp);
        var avgAttack = teamPokemons.Average(t => t.Pokemon.Attack);
        var avgDefense = teamPokemons.Average(t => t.Pokemon.Defense);
        var avgSpecialAttack = teamPokemons.Average(t => t.Pokemon.SpecialAttack);
        var avgSpecialDefense = teamPokemons.Average(t => t.Pokemon.SpecialDefense);
        var avgSpeed = teamPokemons.Average(t => t.Pokemon.Speed);

        var avgTotalStats =
            avgHp +
            avgAttack +
            avgDefense +
            avgSpecialAttack +
            avgSpecialDefense +
            avgSpeed;

        var catalogAverages = await _catalogAverageService.GetAveragesAsync(cancellationToken);

        response.AverageStats = new TeamAverageStatsResponse
        {
            Hp = Math.Round(avgHp, 2),
            Attack = Math.Round(avgAttack, 2),
            Defense = Math.Round(avgDefense, 2),
            SpecialAttack = Math.Round(avgSpecialAttack, 2),
            SpecialDefense = Math.Round(avgSpecialDefense, 2),
            Speed = Math.Round(avgSpeed, 2),
            TotalStats = Math.Round(avgTotalStats, 2)
        };

        var types = teamPokemons
            .SelectMany(t => t.Pokemon.PokemonTypes)
            .Select(pt => pt.PokemonType.Name.ToLowerInvariant())
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        response.Types = types;

        response.MissingRecommendedTypes = RecommendedTypes
            .Where(recommendedType => !types.Contains(recommendedType))
            .ToList();

        var fastest = teamPokemons.MaxBy(t => t.Pokemon.Speed);
        var strongest = teamPokemons.MaxBy(t => t.Pokemon.Attack);
        var bestDefensive = teamPokemons.MaxBy(t => t.Pokemon.Defense);

        if (fastest != null)
        {
            response.FastestPokemon = new TeamTopPokemonResponse
            {
                PokeApiId = fastest.Pokemon.PokeApiId,
                Name = fastest.Pokemon.Name,
                Nickname = fastest.Nickname,
                ImageUrl = fastest.Pokemon.ImageUrl,
                Value = fastest.Pokemon.Speed
            };
        }

        if (strongest != null)
        {
            response.StrongestPokemon = new TeamTopPokemonResponse
            {
                PokeApiId = strongest.Pokemon.PokeApiId,
                Name = strongest.Pokemon.Name,
                Nickname = strongest.Nickname,
                ImageUrl = strongest.Pokemon.ImageUrl,
                Value = strongest.Pokemon.Attack
            };
        }

        if (bestDefensive != null)
        {
            response.BestDefensivePokemon = new TeamTopPokemonResponse
            {
                PokeApiId = bestDefensive.Pokemon.PokeApiId,
                Name = bestDefensive.Pokemon.Name,
                Nickname = bestDefensive.Nickname,
                ImageUrl = bestDefensive.Pokemon.ImageUrl,
                Value = bestDefensive.Pokemon.Defense
            };
        }

        CalculateStrengths(
            response,
            avgSpeed,
            avgAttack,
            avgDefense,
            types.Count);

        CalculateWeaknesses(
            response,
            catalogAverages,
            avgHp,
            avgAttack,
            avgDefense,
            avgSpecialAttack,
            avgSpecialDefense,
            avgSpeed,
            avgTotalStats,
            types.Count,
            response.CurrentTeamSize);

        CalculateRecommendations(
            response,
            types);

        response.TeamScore = CalculateTeamScore(
            response.CurrentTeamSize,
            types.Count,
            avgTotalStats);

        var aiSummary = await GenerateAiSummaryAsync(
            response,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(aiSummary))
        {
            response.Summary = aiSummary;
            response.AiSummaryUsed = true;
        }
        else
        {
            response.Summary = GenerateFallbackSummary(response);
            response.AiSummaryUsed = false;
        }

        return response;
    }

    private static void CalculateStrengths(
        DreamTeamAnalysisResponse response,
        double avgSpeed,
        double avgAttack,
        double avgDefense,
        int distinctTypeCount)
    {
        if (avgSpeed >= 80)
        {
            response.Strengths.Add("This team is fast and can act quickly.");
        }

        if (avgAttack >= 80)
        {
            response.Strengths.Add("This team has strong physical attack potential.");
        }

        if (avgDefense >= 75)
        {
            response.Strengths.Add("This team has solid defensive presence.");
        }

        if (distinctTypeCount >= 4)
        {
            response.Strengths.Add("This team has good type variety.");
        }

        if (response.IsFullTeam)
        {
            response.Strengths.Add("This is a full Dream Team.");
        }

        if (response.Strengths.Count == 0)
        {
            response.Strengths.Add("Your team has potential for growth.");
        }
    }

    private static void CalculateWeaknesses(
        DreamTeamAnalysisResponse response,
        PokemonCatalogAveragesResponse catalogAverages,
        double avgHp,
        double avgAttack,
        double avgDefense,
        double avgSpecialAttack,
        double avgSpecialDefense,
        double avgSpeed,
        double avgTotalStats,
        int distinctTypeCount,
        int currentTeamSize)
    {

       
        if (currentTeamSize < maxTeamSize)
        {
            response.Weaknesses.Add("The team is not full yet.");
        }
        var minimumExpectedTypes = Math.Ceiling(currentTeamSize * 0.6);
        bool needToCheckVariety = currentTeamSize >= Math.Ceiling((maxTeamSize / 2.0));
        if (needToCheckVariety && distinctTypeCount < minimumExpectedTypes)
        {
            response.Weaknesses.Add("The team has limited type variety.");
        }

        if (IsBelowAverage(avgHp, catalogAverages.Hp, out var hpPercentageBelow))
        {
            response.Weaknesses.Add(
                $"HP is {hpPercentageBelow}% below the average Pokémon.");
        }

        if (IsBelowAverage(avgAttack, catalogAverages.Attack, out var attackPercentageBelow))
        {
            response.Weaknesses.Add(
                $"Attack is {attackPercentageBelow}% below the average Pokémon.");
        }

        if (IsBelowAverage(avgDefense, catalogAverages.Defense, out var defensePercentageBelow))
        {
            response.Weaknesses.Add(
                $"Defense is {defensePercentageBelow}% below the average Pokémon.");
        }

        if (IsBelowAverage(avgSpeed, catalogAverages.Speed, out var speedPercentageBelow))
        {
            response.Weaknesses.Add(
                $"Speed is {speedPercentageBelow}% below the average Pokémon.");
        }

        if (IsBelowAverage(avgTotalStats, catalogAverages.TotalStats, out var totalStatsPercentageBelow))
        {
            response.Weaknesses.Add(
                $"Overall stats are {totalStatsPercentageBelow}% below the average Pokémon.");
        }


    }

    private static void CalculateRecommendations(
        DreamTeamAnalysisResponse response,
        List<string> types)
    {
        if (response.CurrentTeamSize < 5)
        {
            response.Recommendations.Add("Add more Pokémon to complete your Dream Team.");
        }

        if (!types.Contains("water"))
        {
            response.Recommendations.Add("Consider adding a Water type for better balance.");
        }

        if (!types.Contains("electric"))
        {
            response.Recommendations.Add("Consider adding an Electric type for speed and coverage.");
        }

        if (response.AverageStats.Defense < 50)
        {
            response.Recommendations.Add("Consider adding a defensive Pokémon with higher Defense or HP.");
        }

        if (response.AverageStats.Speed < 50)
        {
            response.Recommendations.Add("Consider adding a faster Pokémon to improve initiative.");
        }
    }

    private static int CalculateTeamScore(
        int currentTeamSize,
        int distinctTypeCount,
        double avgTotalStats)
    {
        var score = 40;

        score += (int)((currentTeamSize / 5.0) * 20);

        score += (int)(Math.Min(distinctTypeCount, 8) / 8.0 * 20);

        if (avgTotalStats >= 500)
        {
            score += 20;
        }
        else if (avgTotalStats >= 400)
        {
            score += (int)((avgTotalStats - 400) / 100.0 * 20);
        }

        return Math.Clamp(score, 0, 100);
    }

    private async Task<string?> GenerateAiSummaryAsync(
        DreamTeamAnalysisResponse response,
        CancellationToken cancellationToken)
    {
        try
        {
            var prompt = BuildGeminiPrompt(response);

            var result = await _geminiService.GenerateJsonAsync(
                prompt,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(result))
            {
                return null;
            }

            var extractedSummary = ExtractSummaryFromAiResponse(result);

            return string.IsNullOrWhiteSpace(extractedSummary)
                ? null
                : extractedSummary;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to generate AI summary for Dream Team analysis.");

            return null;
        }
    }

    private static string BuildGeminiPrompt(
        DreamTeamAnalysisResponse response)
    {
        var strengthsList = response.Strengths.Count == 0
            ? "none"
            : string.Join(", ", response.Strengths);

        var weaknessesList = response.Weaknesses.Count == 0
            ? "none"
            : string.Join(", ", response.Weaknesses);

        var recommendationsList = response.Recommendations.Count == 0
            ? "none"
            : string.Join(", ", response.Recommendations);

        var typesList = response.Types.Count == 0
            ? "none"
            : string.Join(", ", response.Types);

        return $$"""
You are a friendly Pokémon trainer coach.

Based only on the Dream Team analysis below, write a short, fun, encouraging summary for the trainer.
always return Weaknesses even when it's empty ,if empty analyze the Weaknesses by your self
Rules:
- Return only valid JSON.
- Do not return markdown.
- Do not return explanations outside the JSON.
- Do not invent Pokémon.
- Do not invent stats.
- Use only the provided data.
- Keep it short: 2 sentences maximum.

Schema:
{
  "summary": "short friendly trainer-style summary"
}

Dream Team analysis:
Team Score: {{response.TeamScore}}/100
Team Size: {{response.CurrentTeamSize}}/5
Types: {{typesList}}
Average Total Stats: {{response.AverageStats.TotalStats:F1}}
Average Speed: {{response.AverageStats.Speed:F1}}
Average Attack: {{response.AverageStats.Attack:F1}}
Average Defense: {{response.AverageStats.Defense:F1}}

Strengths: {{strengthsList}}
Weaknesses: {{weaknessesList}}
Recommendations: {{recommendationsList}}
""";
    }

    private static string ExtractSummaryFromAiResponse(
        string value)
    {
        var cleaned = CleanJson(value);

        try
        {
            using var document = JsonDocument.Parse(cleaned);

            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("summary", out var summaryElement))
            {
                return summaryElement.GetString() ?? string.Empty;
            }

            if (root.ValueKind == JsonValueKind.String)
            {
                return root.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // If Gemini returned plain text instead of JSON,
            // use the cleaned value as fallback.
        }

        return cleaned;
    }

    private static string CleanJson(
        string value)
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

    private static string GenerateFallbackSummary(
        DreamTeamAnalysisResponse response)
    {
        return $"Your Dream Team has a score of {response.TeamScore}/100. " +
               $"It has {response.Types.Count} type{(response.Types.Count != 1 ? "s" : "")} " +
               $"and average total stats of {response.AverageStats.TotalStats:F0}. " +
               (response.Recommendations.Count > 0
                   ? $"Consider: {string.Join("; ", response.Recommendations.Take(2))}"
                   : "Keep building your dream team!");
    }
    private static bool IsBelowAverage(
      double teamAverage,
      double catalogAverage,
      out int percentageBelow)
    {
        percentageBelow = 0;

        if (catalogAverage <= 0 || teamAverage >= catalogAverage)
        {
            return false;
        }

        percentageBelow = (int)Math.Round(
            ((catalogAverage - teamAverage) / catalogAverage) * 100);

        return percentageBelow >= 5;
    }
}