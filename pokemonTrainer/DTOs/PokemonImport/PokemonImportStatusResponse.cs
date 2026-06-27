namespace pokemonTrainer.DTOs.PokemonImport;

public class PokemonImportStatusResponse
{
    public string Status { get; set; } = "NotStarted";

    public bool IsReady { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    public int RemoteCount { get; set; }

    public int LocalCountBefore { get; set; }

    public int Checked { get; set; }

    public int Missing { get; set; }

    public int Created { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }

    public string LastMessage { get; set; } = string.Empty;

    public List<string> Errors { get; set; } = new();
}