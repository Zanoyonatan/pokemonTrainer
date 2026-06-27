namespace pokemonTrainer.Models;

public class DreamTeamPokemon
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int PokemonId { get; set; }

    public Pokemon Pokemon { get; set; } = null!;

    public int Slot { get; set; }

    public string? Nickname { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}