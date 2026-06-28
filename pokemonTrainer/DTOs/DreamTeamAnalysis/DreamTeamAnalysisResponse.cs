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
