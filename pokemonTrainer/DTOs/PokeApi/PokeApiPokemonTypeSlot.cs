namespace pokemonTrainer.DTOs.PokeApi;

public class PokeApiPokemonTypeSlot
{
    public int Slot { get; set; }

    public PokeApiNamedResource Type { get; set; } = new();
}