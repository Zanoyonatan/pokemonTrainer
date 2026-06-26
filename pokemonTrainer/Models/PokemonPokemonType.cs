namespace pokemonTrainer.Models;

public class PokemonPokemonType
{
    public int PokemonId { get; set; }

    public Pokemon Pokemon { get; set; } = null!;

    public int PokemonTypeId { get; set; }

    public PokemonType PokemonType { get; set; } = null!;
}