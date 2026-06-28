namespace pokemonTrainer.DTOs.Nicknames;

public class GeneratePokemonNicknamesResponse
{
    public int PokeApiId { get; set; }

    public string PokemonName { get; set; } = string.Empty;

    public List<string> Types { get; set; } = new();

    public List<string> Suggestions { get; set; } = new();

    public bool AiUsed { get; set; }
}
