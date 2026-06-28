using pokemonTrainer.DTOs.Pokemon;

namespace pokemonTrainer.DTOs.AiSearch;

public class PokemonSmartSearchResponse
{
    public PokemonSmartSearchCriteria Criteria { get; set; } = new();

    public PokemonPagedResponse Results { get; set; } = new();

    public string Explanation { get; set; } = string.Empty;
}