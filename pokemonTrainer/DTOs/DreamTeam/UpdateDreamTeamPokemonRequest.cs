using System.ComponentModel.DataAnnotations;

namespace pokemonTrainer.DTOs.DreamTeam;

public class UpdateDreamTeamPokemonRequest
{
    [MaxLength(50)]
    public string? Nickname { get; set; }
}