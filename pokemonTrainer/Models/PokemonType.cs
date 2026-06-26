namespace pokemonTrainer.Models;

public class PokemonType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<PokemonPokemonType> PokemonTypes { get; set; } = new List<PokemonPokemonType>();
}