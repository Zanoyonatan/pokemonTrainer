using pokemonTrainer.DTOs.PokemonImport;

namespace pokemonTrainer.Services;

public enum PokemonImportStatus
{
    NotStarted,
    Running,
    Completed,
    Failed
}

public class PokemonImportStatusService
{
    private readonly object _lock = new();

    private PokemonImportStatusResponse _current = new()
    {
        Status = PokemonImportStatus.NotStarted.ToString(),
        IsReady = false,
        LastMessage = "Pokémon import has not started yet."
    };

    public bool IsReady
    {
        get
        {
            lock (_lock)
            {
                return _current.IsReady;
            }
        }
    }

    public PokemonImportStatusResponse GetStatus()
    {
        lock (_lock)
        {
            return Clone(_current);
        }
    }

    public void MarkRunning()
    {
        lock (_lock)
        {
            _current = new PokemonImportStatusResponse
            {
                Status = PokemonImportStatus.Running.ToString(),
                IsReady = false,
                StartedAtUtc = DateTime.UtcNow,
                LastMessage = "Pokémon import is running."
            };
        }
    }

    public void MarkCompleted(PokemonImportResult result)
    {
        lock (_lock)
        {
            _current = new PokemonImportStatusResponse
            {
                Status = PokemonImportStatus.Completed.ToString(),
                IsReady = true,
                StartedAtUtc = _current.StartedAtUtc,
                FinishedAtUtc = DateTime.UtcNow,

                RemoteCount = result.RemoteCount,
                LocalCountBefore = result.LocalCountBefore,
                Checked = result.Checked,
                Missing = result.Missing,
                Created = result.Created,
                Skipped = result.Skipped,
                Failed = result.Failed,
                Errors = result.Errors.ToList(),

                LastMessage = "Pokémon import completed successfully."
            };
        }
    }

    public void MarkFailed(string message, List<string>? errors = null)
    {
        lock (_lock)
        {
            _current = new PokemonImportStatusResponse
            {
                Status = PokemonImportStatus.Failed.ToString(),
                IsReady = false,
                StartedAtUtc = _current.StartedAtUtc,
                FinishedAtUtc = DateTime.UtcNow,
                LastMessage = message,
                Errors = errors ?? new List<string>()
            };
        }
    }

    private static PokemonImportStatusResponse Clone(
        PokemonImportStatusResponse source)
    {
        return new PokemonImportStatusResponse
        {
            Status = source.Status,
            IsReady = source.IsReady,
            StartedAtUtc = source.StartedAtUtc,
            FinishedAtUtc = source.FinishedAtUtc,

            RemoteCount = source.RemoteCount,
            LocalCountBefore = source.LocalCountBefore,
            Checked = source.Checked,
            Missing = source.Missing,
            Created = source.Created,
            Skipped = source.Skipped,
            Failed = source.Failed,

            LastMessage = source.LastMessage,
            Errors = source.Errors.ToList()
        };
    }
}