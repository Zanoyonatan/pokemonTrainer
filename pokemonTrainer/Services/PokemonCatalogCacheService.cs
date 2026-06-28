using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
using pokemonTrainer.DTOs.Pokemon;
using pokemonTrainer.Models;

namespace pokemonTrainer.Services;

public class PokemonCatalogCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PokemonCatalogCacheService> _logger;

    private const string CatalogCacheKey = "pokemon_catalog";
    private const string CatalogTimestampKey = "pokemon_catalog_timestamp";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    public PokemonCatalogCacheService(
        IMemoryCache memoryCache,
        ApplicationDbContext dbContext,
        ILogger<PokemonCatalogCacheService> logger)
    {
        _memoryCache = memoryCache;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<PokemonCatalogCacheItem>?> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to load from database
            var dbPokemons = await _dbContext.Pokemons
                .AsNoTracking()
                .Include(p => p.PokemonTypes)
                    .ThenInclude(pt => pt.PokemonType)
                .OrderBy(p => p.PokeApiId)
                .ToListAsync(cancellationToken);

            // Convert to cache items
            var cacheItems = dbPokemons
                .Select(MapToCacheItem)
                .ToList();

            // Refresh cache
            RefreshCache(cacheItems);

            return cacheItems;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Pokémon catalog from database. Attempting to use cache.");

            // Try fallback to cache
            if (_memoryCache.TryGetValue(CatalogCacheKey, out List<PokemonCatalogCacheItem>? cachedCatalog))
            {
                _logger.LogInformation("Using cached Pokémon catalog ({Count} items).", cachedCatalog?.Count ?? 0);
                return cachedCatalog;
            }

            return null;
        }
    }

    public List<PokemonCatalogCacheItem>? GetCachedCatalog()
    {
        if (_memoryCache.TryGetValue(CatalogCacheKey, out List<PokemonCatalogCacheItem>? cachedCatalog))
        {
            return cachedCatalog;
        }

        return null;
    }

    public bool IsCatalogCached()
    {
        return _memoryCache.TryGetValue(CatalogCacheKey, out _);
    }

    public void RefreshCache(List<PokemonCatalogCacheItem> catalog)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheExpiration);

        _memoryCache.Set(CatalogCacheKey, catalog, cacheEntryOptions);
        _memoryCache.Set(CatalogTimestampKey, DateTime.UtcNow, cacheEntryOptions);

        _logger.LogInformation("Pokémon catalog cache refreshed with {Count} items.", catalog.Count);
    }

    public PokemonCatalogCacheItem? FindInCache(int pokeApiId)
    {
        var cached = GetCachedCatalog();
        return cached?.FirstOrDefault(p => p.PokeApiId == pokeApiId);
    }

    private static PokemonCatalogCacheItem MapToCacheItem(Pokemon pokemon)
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
                .Select(pt => pt.PokemonType.Name)
                .ToList()
        };
    }
}
