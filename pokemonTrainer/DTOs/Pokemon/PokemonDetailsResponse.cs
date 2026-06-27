namespace pokemonTrainer.DTOs.Pokemon;

public class PokemonDetailsResponse
{
    public int Id { get; set; }

    public int PokeApiId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int Height { get; set; }

    public int Weight { get; set; }

    public int? BaseExperience { get; set; }

    public bool IsLegendary { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<string> Types { get; set; } = new();

    public List<PokemonStatResponse> Stats { get; set; } = new();

    public List<PokemonAbilityResponse> Abilities { get; set; } = new();
}