using System.ComponentModel.DataAnnotations;

namespace pokemonTrainer.DTOs.Nicknames;

public class GeneratePokemonNicknamesRequest
{
    [Range(1, 10)]
    public int Count { get; set; } = 5;
}
