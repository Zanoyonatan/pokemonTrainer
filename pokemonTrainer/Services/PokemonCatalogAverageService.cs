using Microsoft.Extensions.Caching.Memory;
using pokemonTrainer.DTOs.Pokemon;

namespace pokemonTrainer.Services;

public sealed class PokemonCatalogAverageService
{
    private const string CacheKey = "pokemon-catalog-averages";

    private readonly PokemonCatalogCacheService _catalogCacheService;
    private readonly IMemoryCache _cache;

    public PokemonCatalogAverageService(
        PokemonCatalogCacheService catalogCacheService,
        IMemoryCache cache)
    {
        _catalogCacheService = catalogCacheService;
        _cache = cache;
    }

    public async Task<PokemonCatalogAveragesResponse> GetAveragesAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out PokemonCatalogAveragesResponse? cached) &&
            cached is not null)
        {
            return cached;
        }

        var catalog = await _catalogCacheService.GetCatalogAsync(cancellationToken);

        if (catalog.Count == 0)
        {
            return new PokemonCatalogAveragesResponse();
        }

        var averages = new PokemonCatalogAveragesResponse
        {
            Hp = Math.Round(catalog.Average(p => p.Hp), 2),
            Attack = Math.Round(catalog.Average(p => p.Attack), 2),
            Defense = Math.Round(catalog.Average(p => p.Defense), 2),
            SpecialAttack = Math.Round(catalog.Average(p => p.SpecialAttack), 2),
            SpecialDefense = Math.Round(catalog.Average(p => p.SpecialDefense), 2),
            Speed = Math.Round(catalog.Average(p => p.Speed), 2),
            TotalStats = Math.Round(catalog.Average(p =>
                p.Hp +
                p.Attack +
                p.Defense +
                p.SpecialAttack +
                p.SpecialDefense +
                p.Speed), 2)
        };

        _cache.Set(CacheKey, averages, TimeSpan.FromDays(7));

        return averages;
    }

    public void ClearCache()
    {
        _cache.Remove(CacheKey);
    }
}