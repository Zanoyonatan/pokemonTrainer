using System.Text.Json.Serialization;

namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiPokemonStatSlot
{
    [JsonPropertyName("base_stat")]
    public int BaseStat { get; set; }

    public int Effort { get; set; }

    public PokeApiNamedResource Stat { get; set; } = new();
}