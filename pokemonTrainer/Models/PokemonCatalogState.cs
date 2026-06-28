namespace pokemonTrainer.Models;

public class PokemonCatalogState
{
    public int Id { get; set; }

    public int LastKnownRemoteCount { get; set; }

    public int LocalCountAtLastSuccessfulImport { get; set; }

    public bool IsComplete { get; set; }

    public DateTime? LastSuccessfulImportAtUtc { get; set; }

    public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;
}