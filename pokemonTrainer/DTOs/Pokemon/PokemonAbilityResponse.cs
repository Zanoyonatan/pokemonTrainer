namespace pokemonTrainer.DTOs.Pokemon;

public class PokemonAbilityResponse
{
    public string Name { get; set; } = string.Empty;

    public bool IsHidden { get; set; }

    public int Slot { get; set; }
}