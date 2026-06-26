namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiListResponse
{
    public int Count { get; set; }

    public string? Next { get; set; }

    public string? Previous { get; set; }

    public List<PokeApiNamedResource> Results { get; set; } = new();
}