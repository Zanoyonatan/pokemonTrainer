using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
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
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var hasUsableLocalCatalog = await HasUsableLocalCatalogAsync(
            dbContext,
            stoppingToken);

        _statusService.MarkRunning(
            keepExistingDataAvailable: hasUsableLocalCatalog);

        try
        {
            var importService = scope.ServiceProvider
                .GetRequiredService<PokemonImportService>();

            _logger.LogInformation(
                "Starting Pokémon startup import. HasUsableLocalCatalog: {HasUsableLocalCatalog}",
                hasUsableLocalCatalog);

            var result = await importService.ImportMissingAsync(
                maxCount: null,
                cancellationToken: stoppingToken);

            _statusService.MarkCompleted(
                result,
                keepExistingDataAvailable: hasUsableLocalCatalog);

            _logger.LogInformation(
                "Pokémon startup import completed. Remote: {RemoteCount}, LocalBefore: {LocalCountBefore}, LocalAfter: {LocalCountAfter}, IsComplete: {IsComplete}",
                result.RemoteCount,
                result.LocalCountBefore,
                result.LocalCountAfter,
                result.IsComplete);
        }
        catch (TaskCanceledException ex) when (!stoppingToken.IsCancellationRequested)
        {
            const string message = "Pokémon startup import timed out.";

            _statusService.MarkFailed(
                message,
                BuildErrors(ex),
                keepExistingDataAvailable: hasUsableLocalCatalog);

            _logger.LogWarning(ex, message);
        }
        catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
        {
            const string message = "Pokémon startup import was cancelled because the application is stopping.";

            _statusService.MarkFailed(
                message,
                BuildErrors(ex),
                keepExistingDataAvailable: hasUsableLocalCatalog);

            _logger.LogInformation(ex, message);
        }
        catch (Exception ex)
        {
            const string message = "Pokémon startup import failed.";

            _statusService.MarkFailed(
                message,
                BuildErrors(ex),
                keepExistingDataAvailable: hasUsableLocalCatalog);

            _logger.LogError(ex, message);
        }
    }

    private static async Task<bool> HasUsableLocalCatalogAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var localPokemonCount = await dbContext.Pokemons
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var catalogState = await dbContext.PokemonCatalogStates
            .AsNoTracking()
            .OrderByDescending(s => s.LastSuccessfulImportAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return catalogState != null &&
               catalogState.IsComplete &&
               localPokemonCount >= catalogState.LastKnownRemoteCount;
    }

    private static List<string> BuildErrors(Exception ex)
    {
        var errors = new List<string>
        {
            ex.Message
        };

        var innerException = ex.InnerException;

        while (innerException != null)
        {
            errors.Add(innerException.Message);
            innerException = innerException.InnerException;
        }

        return errors;
    }
}