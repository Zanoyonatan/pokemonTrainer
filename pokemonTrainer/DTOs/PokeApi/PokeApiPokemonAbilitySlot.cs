using System.Text.Json.Serialization;

namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiPokemonAbilitySlot
{
    [JsonPropertyName("is_hidden")]
    public bool IsHidden { get; set; }

    public int Slot { get; set; }

    public PokeApiNamedResource Ability { get; set; } = new();
}