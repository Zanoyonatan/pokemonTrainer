namespace pokemonTrainer.DTOs.DreamTeamAnalysis;

public class TeamTopPokemonResponse
{
    public int PokeApiId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Nickname { get; set; }

    public string? ImageUrl { get; set; }

    public int Value { get; set; }
}
