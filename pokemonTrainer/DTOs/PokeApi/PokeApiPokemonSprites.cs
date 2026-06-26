using System.Text.Json.Serialization;

namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiPokemonSprites
{
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }

    [JsonPropertyName("other")]
    public PokeApiPokemonSpriteOther? Other { get; set; }
}

public class PokeApiPokemonSpriteOther
{
    [JsonPropertyName("official-artwork")]
    public PokeApiPokemonOfficialArtwork? OfficialArtwork { get; set; }
}

public class PokeApiPokemonOfficialArtwork
{
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }
}