using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
using pokemonTrainer.DTOs.PokeApi;
using pokemonTrainer.DTOs.PokemonImport;
using pokemonTrainer.Infrastructure;
using pokemonTrainer.Models;

namespace pokemonTrainer.Services;

public class PokemonImportService
{
    private const int PageSize = 200;
    private const int SaveBatchSize = 50;

    private readonly ApplicationDbContext _dbContext;
    private readonly PokeApiClient _pokeApiClient;
    private readonly PokemonCatalogCacheService _cacheService;
    private readonly ILogger<PokemonImportService> _logger;

    public PokemonImportService(
        ApplicationDbContext dbContext,
        PokeApiClient pokeApiClient,
        PokemonCatalogCacheService cacheService,
        ILogger<PokemonImportService> logger)
    {
        _dbContext = dbContext;
        _pokeApiClient = pokeApiClient;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<PokemonImportResult> ImportMissingAsync(
        int? maxCount = null,
        CancellationToken cancellationToken = default)
    {
        var result = new PokemonImportResult();

        var remoteCount = await GetRemotePokemonCountAsync(cancellationToken);

        var localCountBefore = await _dbContext.Pokemons
            .CountAsync(cancellationToken);

        result.RemoteCount = remoteCount;
        result.LocalCountBefore = localCountBefore;

        if (!maxCount.HasValue && localCountBefore == remoteCount)
        {
            result.Checked = 0;
            result.Missing = 0;
            result.Created = 0;
            result.Failed = 0;
            result.Skipped = remoteCount;
            result.LocalCountAfter = localCountBefore;
            result.IsComplete = true;

            await UpdateCatalogStateIfCompleteAsync(
                result,
                cancellationToken);

            await _cacheService.RefreshIfCountChangedAsync(
                expectedDatabaseCount: result.LocalCountAfter,
                cancellationToken);

            _logger.LogInformation(
                "Local Pokémon count matches remote count. Import skipped. Count: {Count}",
                remoteCount);

            return result;
        }

        var remoteReferences = await LoadRemotePokemonReferencesAsync(
            maxCount,
            cancellationToken);

        result.RemoteCount = remoteReferences.RemoteCount;
        result.Checked = remoteReferences.Items.Count;

        var localIdsList = await _dbContext.Pokemons
            .Select(p => p.PokeApiId)
            .ToListAsync(cancellationToken);

        var localIds = localIdsList.ToHashSet();

        result.LocalCountBefore = localIds.Count;

        var missingReferences = remoteReferences.Items
            .Where(p => !localIds.Contains(p.PokeApiId))
            .OrderBy(p => p.PokeApiId)
            .ToList();

        result.Missing = missingReferences.Count;
        result.Skipped = remoteReferences.Items.Count - missingReferences.Count;

        if (missingReferences.Count == 0)
        {
            result.LocalCountAfter = await _dbContext.Pokemons
                .CountAsync(cancellationToken);

            result.IsComplete =
                !maxCount.HasValue &&
                result.Failed == 0 &&
                result.LocalCountAfter >= result.RemoteCount;

            await UpdateCatalogStateIfCompleteAsync(
                result,
                cancellationToken);

            await _cacheService.RefreshIfCountChangedAsync(
                expectedDatabaseCount: result.LocalCountAfter,
                cancellationToken);

            _logger.LogInformation(
                "No missing Pokémon found. Import skipped.");

            return result;
        }

        _logger.LogInformation(
            "Starting Pokémon import. RemoteCount: {RemoteCount}, LocalCount: {LocalCount}, Missing: {Missing}",
            result.RemoteCount,
            result.LocalCountBefore,
            result.Missing);

        var existingTypes = await _dbContext.PokemonTypes
            .ToDictionaryAsync(t => t.Name, cancellationToken);

        var processedSinceLastSave = 0;

        foreach (var reference in missingReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var details = await _pokeApiClient.GetPokemonDetailsAsync(
                    reference.PokeApiId.ToString(),
                    cancellationToken);

                if (details == null)
                {
                    result.Failed++;
                    result.Errors.Add(
                        $"Failed to load details for Pokémon ID {reference.PokeApiId}.");

                    continue;
                }

                if (localIds.Contains(details.Id))
                {
                    result.Skipped++;
                    continue;
                }

                var pokemon = CreatePokemon(details);

                foreach (var typeSlot in details.Types.OrderBy(t => t.Slot))
                {
                    var typeName = typeSlot.Type.Name.ToLowerInvariant();

                    if (!existingTypes.TryGetValue(typeName, out var pokemonType))
                    {
                        pokemonType = new PokemonType
                        {
                            Name = typeName
                        };

                        _dbContext.PokemonTypes.Add(pokemonType);
                        existingTypes[typeName] = pokemonType;
                    }

                    pokemon.PokemonTypes.Add(new PokemonPokemonType
                    {
                        Pokemon = pokemon,
                        PokemonType = pokemonType
                    });
                }

                _dbContext.Pokemons.Add(pokemon);

                localIds.Add(pokemon.PokeApiId);
                result.Created++;
                processedSinceLastSave++;

                if (processedSinceLastSave >= SaveBatchSize)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    processedSinceLastSave = 0;

                    _logger.LogInformation(
                        "Pokémon import progress. Created so far: {Created}, Failed: {Failed}",
                        result.Created,
                        result.Failed);
                }
            }
            catch (Exception ex)
            {
                result.Failed++;

                var error =
                    $"Failed to import Pokémon ID {reference.PokeApiId} ({reference.Name}): {ex.Message}";

                result.Errors.Add(error);

                _logger.LogError(
                    ex,
                    error);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        result.LocalCountAfter = await _dbContext.Pokemons
            .CountAsync(cancellationToken);

        result.IsComplete =
            !maxCount.HasValue &&
            result.Failed == 0 &&
            result.LocalCountAfter >= result.RemoteCount;

        await UpdateCatalogStateIfCompleteAsync(
            result,
            cancellationToken);

        if (result.LocalCountAfter > 0)
        {
            await _cacheService.RefreshIfCountChangedAsync(
                expectedDatabaseCount: result.LocalCountAfter,
                cancellationToken);
        }

        _logger.LogInformation(
            "Pokémon import completed. Created: {Created}, Skipped: {Skipped}, Failed: {Failed}, IsComplete: {IsComplete}",
            result.Created,
            result.Skipped,
            result.Failed,
            result.IsComplete);

        return result;
    }

    private async Task UpdateCatalogStateIfCompleteAsync(
        PokemonImportResult result,
        CancellationToken cancellationToken)
    {
        if (!result.IsComplete)
        {
            return;
        }

        var state = await _dbContext.PokemonCatalogStates
            .FirstOrDefaultAsync(cancellationToken);

        if (state == null)
        {
            state = new PokemonCatalogState();

            _dbContext.PokemonCatalogStates.Add(state);
        }

        state.LastKnownRemoteCount = result.RemoteCount;
        state.LocalCountAtLastSuccessfulImport = result.LocalCountAfter;
        state.IsComplete = true;
        state.LastSuccessfulImportAtUtc = DateTime.UtcNow;
        state.LastUpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> GetRemotePokemonCountAsync(
        CancellationToken cancellationToken)
    {
        var firstPage = await _pokeApiClient.GetPokemonListAsync(
            limit: 1,
            offset: 0,
            cancellationToken);

        if (firstPage == null)
        {
            throw new InvalidOperationException(
                "Failed to load Pokémon count from PokeAPI.");
        }

        return firstPage.Count;
    }

    private async Task<RemotePokemonReferencesResult> LoadRemotePokemonReferencesAsync(
        int? maxCount,
        CancellationToken cancellationToken)
    {
        var firstPageLimit = maxCount.HasValue
            ? Math.Min(maxCount.Value, PageSize)
            : PageSize;

        var firstPage = await _pokeApiClient.GetPokemonListAsync(
            firstPageLimit,
            offset: 0,
            cancellationToken);

        if (firstPage == null)
        {
            throw new InvalidOperationException(
                "Failed to load Pokémon list from PokeAPI.");
        }

        var remoteCount = firstPage.Count;

        var targetCount = maxCount.HasValue
            ? Math.Min(maxCount.Value, remoteCount)
            : remoteCount;

        var items = new List<RemotePokemonReference>();

        AddReferencesFromPage(
            firstPage,
            items);

        var offset = items.Count;

        while (items.Count < targetCount)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remaining = targetCount - items.Count;
            var limit = Math.Min(PageSize, remaining);

            var page = await _pokeApiClient.GetPokemonListAsync(
                limit,
                offset,
                cancellationToken);

            if (page == null)
            {
                throw new InvalidOperationException(
                    $"Failed to load Pokémon list page at offset {offset}.");
            }

            AddReferencesFromPage(
                page,
                items);

            offset += limit;
        }

        return new RemotePokemonReferencesResult
        {
            RemoteCount = remoteCount,
            Items = items
                .Where(i => i.PokeApiId > 0)
                .GroupBy(i => i.PokeApiId)
                .Select(g => g.First())
                .OrderBy(i => i.PokeApiId)
                .ToList()
        };
    }

    private static void AddReferencesFromPage(
        PokeApiListResponse page,
        List<RemotePokemonReference> items)
    {
        foreach (var item in page.Results)
        {
            var id = ExtractIdFromUrl(item.Url);

            if (id == null)
            {
                continue;
            }

            items.Add(new RemotePokemonReference
            {
                PokeApiId = id.Value,
                Name = item.Name
            });
        }
    }

    private static int? ExtractIdFromUrl(
        string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmedUrl = url.TrimEnd('/');

        var lastSlashIndex = trimmedUrl.LastIndexOf('/');

        if (lastSlashIndex < 0 ||
            lastSlashIndex == trimmedUrl.Length - 1)
        {
            return null;
        }

        var idPart = trimmedUrl[(lastSlashIndex + 1)..];

        return int.TryParse(idPart, out var id)
            ? id
            : null;
    }

    private static int GetBaseStat(
        PokeApiPokemonDetails details,
        string statName)
    {
        return details.Stats
            .FirstOrDefault(s =>
                s.Stat.Name.Equals(
                    statName,
                    StringComparison.OrdinalIgnoreCase))
            ?.BaseStat ?? 0;
    }

    private static Pokemon CreatePokemon(
        PokeApiPokemonDetails details)
    {
        var imageUrl =
            details.Sprites.Other?.OfficialArtwork?.FrontDefault
            ?? details.Sprites.FrontDefault;

        var statsJson = JsonSerializer.Serialize(
            details.Stats.Select(s => new
            {
                Name = s.Stat.Name,
                BaseStat = s.BaseStat,
                s.Effort
            }));

        var abilitiesJson = JsonSerializer.Serialize(
            details.Abilities.Select(a => new
            {
                Name = a.Ability.Name,
                a.IsHidden,
                a.Slot
            }));

        return new Pokemon
        {
            PokeApiId = details.Id,
            Name = details.Name,
            ImageUrl = imageUrl,
            Height = details.Height,
            Weight = details.Weight,
            BaseExperience = details.BaseExperience,

            Hp = GetBaseStat(details, "hp"),
            Attack = GetBaseStat(details, "attack"),
            Defense = GetBaseStat(details, "defense"),
            SpecialAttack = GetBaseStat(details, "special-attack"),
            SpecialDefense = GetBaseStat(details, "special-defense"),
            Speed = GetBaseStat(details, "speed"),

            StatsJson = statsJson,
            AbilitiesJson = abilitiesJson,
            IsLegendary = false
        };
    }

    private class RemotePokemonReferencesResult
    {
        public int RemoteCount { get; set; }

        public List<RemotePokemonReference> Items { get; set; } = new();
    }

    private class RemotePokemonReference
    {
        public int PokeApiId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}