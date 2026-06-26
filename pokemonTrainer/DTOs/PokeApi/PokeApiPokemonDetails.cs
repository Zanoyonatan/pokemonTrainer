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