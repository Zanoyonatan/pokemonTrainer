using pokemonTrainer.DTOs.PokemonImport;

namespace pokemonTrainer.Services;

public enum PokemonImportStatus
{
    NotStarted,
    Running,
    Completed,
    CompletedWithWarnings,
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

    public void MarkRunning(bool keepExistingDataAvailable = false)
    {
        lock (_lock)
        {
            _current = new PokemonImportStatusResponse
            {
                Status = PokemonImportStatus.Running.ToString(),
                IsReady = keepExistingDataAvailable,
                StartedAtUtc = DateTime.UtcNow,
                LastMessage = keepExistingDataAvailable
                    ? "Pokémon import is running in the background. Existing local data is available."
                    : "Pokémon import is running."
            };
        }
    }

    public void MarkCompleted(
        PokemonImportResult result,
        bool keepExistingDataAvailable = false)
    {
        lock (_lock)
        {
            var isReady = result.IsComplete || keepExistingDataAvailable;

            _current = new PokemonImportStatusResponse
            {
                Status = result.IsComplete
                    ? PokemonImportStatus.Completed.ToString()
                    : PokemonImportStatus.CompletedWithWarnings.ToString(),

                IsReady = isReady,
                IsComplete = result.IsComplete,

                StartedAtUtc = _current.StartedAtUtc,
                FinishedAtUtc = DateTime.UtcNow,

                RemoteCount = result.RemoteCount,
                LocalCountBefore = result.LocalCountBefore,
                LocalCountAfter = result.LocalCountAfter,

                Checked = result.Checked,
                Missing = result.Missing,
                Created = result.Created,
                Skipped = result.Skipped,
                Failed = result.Failed,

                LastMessage = result.IsComplete
                    ? "Pokémon import completed successfully."
                    : keepExistingDataAvailable
                        ? "Pokémon import completed with warnings. Existing local Pokémon data is still available."
                        : "Pokémon import completed, but the local catalog is not complete yet.",

                Errors = result.Errors.ToList()
            };
        }
    }

    public void MarkFailed(
        string message,
        List<string>? errors = null,
        bool keepExistingDataAvailable = false)
    {
        lock (_lock)
        {
            _current = new PokemonImportStatusResponse
            {
                Status = PokemonImportStatus.Failed.ToString(),
                IsReady = keepExistingDataAvailable,
                StartedAtUtc = _current.StartedAtUtc,
                FinishedAtUtc = DateTime.UtcNow,

                LastMessage = keepExistingDataAvailable
                    ? $"{message} Existing local Pokémon data is still available."
                    : message,

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
            IsComplete = source.IsComplete,

            StartedAtUtc = source.StartedAtUtc,
            FinishedAtUtc = source.FinishedAtUtc,

            RemoteCount = source.RemoteCount,
            LocalCountBefore = source.LocalCountBefore,
            LocalCountAfter = source.LocalCountAfter,

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