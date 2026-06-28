using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pokemonTrainer.Data;
using pokemonTrainer.DTOs.Pokemon;
using pokemonTrainer.Models;

namespace pokemonTrainer.Services;

public class PokemonCatalogCacheService
{
    private const string CatalogCacheKey = "pokemon_catalog";
    private const string CatalogTimestampKey = "pokemon_catalog_timestamp";

    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    private readonly IMemoryCache _memoryCache;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PokemonCatalogCacheService> _logger;

    public PokemonCatalogCacheService(
        IMemoryCache memoryCache,
        ApplicationDbContext dbContext,
        ILogger<PokemonCatalogCacheService> logger)
    {
        _memoryCache = memoryCache;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<PokemonCatalogCacheItem>> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        if (TryGetCachedCatalog(out var cachedCatalog))
        {
            return cachedCatalog;
        }

        _logger.LogInformation(
            "Pokemon catalog cache is empty. Loading catalog from database.");

        return await RefreshFromDatabaseAsync(cancellationToken);
    }

    public bool TryGetCachedCatalog(
        out List<PokemonCatalogCacheItem> catalog)
    {
        if (_memoryCache.TryGetValue(
                CatalogCacheKey,
                out List<PokemonCatalogCacheItem>? cachedCatalog) &&
            cachedCatalog is { Count: > 0 })
        {
            catalog = cachedCatalog;
            return true;
        }

        catalog = new List<PokemonCatalogCacheItem>();
        return false;
    }

    public List<PokemonCatalogCacheItem>? GetCachedCatalog()
    {
        return TryGetCachedCatalog(out var catalog)
            ? catalog
            : null;
    }

    public bool IsCatalogCached()
    {
        return TryGetCachedCatalog(out _);
    }

    public int GetCacheCount()
    {
        return TryGetCachedCatalog(out var catalog)
            ? catalog.Count
            : 0;
    }

    public DateTime? GetLastRefreshUtc()
    {
        if (_memoryCache.TryGetValue(
                CatalogTimestampKey,
                out DateTime timestamp))
        {
            return timestamp;
        }

        return null;
    }

    public async Task RefreshIfCountChangedAsync(
        int expectedDatabaseCount,
        CancellationToken cancellationToken = default)
    {
        var cacheCount = GetCacheCount();

        if (cacheCount == expectedDatabaseCount && cacheCount > 0)
        {
            _logger.LogInformation(
                "Pokemon catalog cache is already up to date. CacheCount: {CacheCount}, DbCount: {DbCount}",
                cacheCount,
                expectedDatabaseCount);

            return;
        }

        _logger.LogInformation(
            "Pokemon catalog cache count differs from database. CacheCount: {CacheCount}, DbCount: {DbCount}. Refreshing cache.",
            cacheCount,
            expectedDatabaseCount);

        await RefreshFromDatabaseAsync(cancellationToken);
    }

    public async Task<List<PokemonCatalogCacheItem>> RefreshFromDatabaseAsync(
        CancellationToken cancellationToken = default)
    {
        await RefreshLock.WaitAsync(cancellationToken);

        try
        {
            var pokemons = await _dbContext.Pokemons
                .AsNoTracking()
                .Include(p => p.PokemonTypes)
                    .ThenInclude(pt => pt.PokemonType)
                .OrderBy(p => p.PokeApiId)
                .ToListAsync(cancellationToken);

            var cacheItems = pokemons
                .Select(MapToCacheItem)
                .ToList();

            SetCache(cacheItems);

            _logger.LogInformation(
                "Pokemon catalog cache refreshed successfully. Count: {Count}",
                cacheItems.Count);

            return cacheItems;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    public void RefreshCache(
        List<PokemonCatalogCacheItem> catalog)
    {
        SetCache(catalog);

        _logger.LogInformation(
            "Pokemon catalog cache refreshed manually. Count: {Count}",
            catalog.Count);
    }

    public async Task<PokemonCatalogCacheItem?> FindAsync(
        int pokeApiId,
        CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(cancellationToken);

        return catalog.FirstOrDefault(p =>
            p.PokeApiId == pokeApiId);
    }

    public PokemonCatalogCacheItem? FindInCache(
        int pokeApiId)
    {
        var cached = GetCachedCatalog();

        return cached?.FirstOrDefault(p =>
            p.PokeApiId == pokeApiId);
    }

    private void SetCache(
        List<PokemonCatalogCacheItem> catalog)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.High
        };

        _memoryCache.Set(
            CatalogCacheKey,
            catalog,
            cacheEntryOptions);

        _memoryCache.Set(
            CatalogTimestampKey,
            DateTime.UtcNow,
            cacheEntryOptions);
    }

    private static PokemonCatalogCacheItem MapToCacheItem(
        Pokemon pokemon)
    {
        return new PokemonCatalogCacheItem
        {
            Id = pokemon.Id,
            PokeApiId = pokemon.PokeApiId,
            Name = pokemon.Name,
            ImageUrl = pokemon.ImageUrl,
            Height = pokemon.Height,
            Weight = pokemon.Weight,
            BaseExperience = pokemon.BaseExperience,
            Hp = pokemon.Hp,
            Attack = pokemon.Attack,
            Defense = pokemon.Defense,
            SpecialAttack = pokemon.SpecialAttack,
            SpecialDefense = pokemon.SpecialDefense,
            Speed = pokemon.Speed,
            IsLegendary = pokemon.IsLegendary,
            Types = pokemon.PokemonTypes
                .Select(pt => pt.PokemonType.Name.ToLowerInvariant())
                .Distinct()
                .OrderBy(t => t)
                .ToList()
        };
    }
}