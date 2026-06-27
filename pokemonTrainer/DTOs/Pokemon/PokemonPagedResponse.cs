namespace pokemonTrainer.DTOs.Pokemon;

public class PokemonPagedResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public List<PokemonListItemResponse> Items { get; set; } = new();
}