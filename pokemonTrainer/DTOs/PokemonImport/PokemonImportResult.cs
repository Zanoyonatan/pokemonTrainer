namespace pokemonTrainer.DTOs.PokemonImport;

public class PokemonImportResult
{
    public int RemoteCount { get; set; }

    public int LocalCountBefore { get; set; }
    public int LocalCountAfter { get; set; }

    public int Checked { get; set; }

    public int Missing { get; set; }

    public int Created { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }
    public bool IsComplete { get; set; }
    public List<string> Errors { get; set; } = new();

}