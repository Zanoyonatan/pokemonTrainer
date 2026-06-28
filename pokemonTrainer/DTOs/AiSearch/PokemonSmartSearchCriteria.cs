namespace pokemonTrainer.DTOs.AiSearch;

public class PokemonSmartSearchCriteria
{
    public string OriginalQuery { get; set; } = string.Empty;

    public string? Type { get; set; }

    public string? NameSearch { get; set; }

    public string? SortBy { get; set; }

    public string SortDirection { get; set; } = "asc";

    public int? MinHeight { get; set; }

    public int? MaxHeight { get; set; }

    public int? MinWeight { get; set; }

    public int? MaxWeight { get; set; }

    public List<string> DetectedIntents { get; set; } = new();
}