namespace pokemonTrainer.DTOs.Pokemon;

public class PokemonStatResponse
{
    public string Name { get; set; } = string.Empty;

    public int BaseStat { get; set; }

    public int Effort { get; set; }
}