using pokemonTrainer.Services;

namespace pokemonTrainer.Workers;

public class PokemonStartupImportWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PokemonImportStatusService _statusService;
    private readonly ILogger<PokemonStartupImportWorker> _logger;

    public PokemonStartupImportWorker(
        IServiceScopeFactory scopeFactory,
        PokemonImportStatusService statusService,
        ILogger<PokemonStartupImportWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _statusService = statusService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _statusService.MarkRunning();

        _logger.LogInformation("PokemonStartupImportWorker started.");

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var importService = scope.ServiceProvider
                .GetRequiredService<PokemonImportService>();

            _logger.LogInformation("Starting Pokémon startup import.");

            var result = await importService.ImportMissingAsync(
                maxCount: null,
                cancellationToken: stoppingToken);

            _statusService.MarkCompleted(result);

            _logger.LogInformation(
                "Pokémon startup import completed. Remote: {RemoteCount}, LocalBefore: {LocalCountBefore}, Checked: {Checked}, Missing: {Missing}, Created: {Created}, Skipped: {Skipped}, Failed: {Failed}",
                result.RemoteCount,
                result.LocalCountBefore,
                result.Checked,
                result.Missing,
                result.Created,
                result.Skipped,
                result.Failed);
        }
        catch (OperationCanceledException)
        {
            const string message = "Pokémon startup import was cancelled.";

            _statusService.MarkFailed(message);

            _logger.LogInformation(message);
        }
        catch (Exception ex)
        {
            const string message = "Pokémon startup import failed.";

            _statusService.MarkFailed(
                message,
                new List<string> { ex.Message });

            _logger.LogError(ex, message);
        }
    }
}