namespace pokemonTrainer.DTOs.Pokemon;

public class PokemonListItemResponse
{
    public int Id { get; set; }

    public int PokeApiId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int Height { get; set; }

    public int Weight { get; set; }

    public int? BaseExperience { get; set; }
    public int Hp { get; set; }

    public int Attack { get; set; }

    public int Defense { get; set; }

    public int SpecialAttack { get; set; }

    public int SpecialDefense { get; set; }

    public int Speed { get; set; }
    public bool IsLegendary { get; set; }

    public List<string> Types { get; set; } = new();
}