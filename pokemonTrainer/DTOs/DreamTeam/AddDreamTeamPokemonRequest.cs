using System.ComponentModel.DataAnnotations;

namespace pokemonTrainer.DTOs.DreamTeam;

public class AddDreamTeamPokemonRequest
{
    [Required]
    public int PokeApiId { get; set; }

    [MaxLength(50)]
    public string? Nickname { get; set; }
}