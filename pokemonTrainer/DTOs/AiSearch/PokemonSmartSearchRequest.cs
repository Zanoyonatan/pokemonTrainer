using System.ComponentModel.DataAnnotations;

namespace pokemonTrainer.DTOs.AiSearch;

public class PokemonSmartSearchRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(300)]
    public string Query { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}