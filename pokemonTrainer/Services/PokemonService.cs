using pokemonTrainer.DTOs.AiSearch;
using pokemonTrainer.DTOs.Common;
using pokemonTrainer.DTOs.Pokemon;
using pokemonTrainer.Models;

namespace pokemonTrainer.Services;

public class PokemonService
{
    private readonly PokemonCatalogCacheService _cacheService;

    public PokemonService(
        PokemonCatalogCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<PokemonPagedResponse> GetPagedAsync(
        string? search,
        string? type,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var catalog = await GetCatalogOrThrowAsync(cancellationToken);

        var query = catalog.AsEnumerable();

        query = ApplySearchFilter(query, search);
        query = ApplyTypeFilter(query, type);
        query = ApplySorting(query, sortBy);

        return BuildPagedResponseFromCache(
            query,
            page,
            pageSize);
    }

    public async Task<PokemonPagedResponse> SearchByCriteriaAsync(
        PokemonSmartSearchCriteria criteria,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var catalog = await GetCatalogOrThrowAsync(cancellationToken);

        var query = catalog.AsEnumerable();

        query = ApplySearchFilter(query, criteria.NameSearch);
        query = ApplyTypeFilter(query, criteria.Type);
        query = ApplySizeFilters(query, criteria);
        query = ApplySorting(query, criteria);
        query = ApplyRequestedCount(query, criteria);

        return BuildPagedResponseFromCache(
            query,
            page,
            pageSize);
    }

    public async Task<List<string>> GetTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogOrThrowAsync(cancellationToken);

        return catalog
            .SelectMany(p => p.Types)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();
    }

    public async Task<ServiceResult<PokemonDetailsResponse>> GetByPokeApiIdAsync(
        int pokeApiId,
        CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogOrThrowAsync(cancellationToken);

        var pokemon = catalog.FirstOrDefault(p =>
            p.PokeApiId == pokeApiId);

        if (pokemon == null)
        {
            return ServiceResult<PokemonDetailsResponse>.Fail(
                "POKEMON_NOT_FOUND",
                $"Pokémon with PokeApiId {pokeApiId} was not found.");
        }

        return ServiceResult<PokemonDetailsResponse>.Ok(
            MapCacheItemToDetails(pokemon));
    }

    private async Task<List<PokemonCatalogCacheItem>> GetCatalogOrThrowAsync(
        CancellationToken cancellationToken)
    {
        var catalog = await _cacheService.GetCatalogAsync(cancellationToken);

        if (catalog is { Count: > 0 })
        {
            return catalog;
        }

        throw new InvalidOperationException(
            "The Pokémon catalog is unavailable. Cache is empty and the database could not provide the catalog.");
    }

    private static IEnumerable<PokemonCatalogCacheItem> ApplySearchFilter(
        IEnumerable<PokemonCatalogCacheItem> query,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var normalizedSearch = search.Trim();

        if (int.TryParse(normalizedSearch, out var pokeApiId))
        {
            return query.Where(p =>
                p.PokeApiId == pokeApiId ||
                p.Name.Contains(
                    normalizedSearch,
                    StringComparison.OrdinalIgnoreCase));
        }

        return query.Where(p =>
            p.Name.Contains(
                normalizedSearch,
                StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<PokemonCatalogCacheItem> ApplyTypeFilter(
        IEnumerable<PokemonCatalogCacheItem> query,
        string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return query;
        }

        var normalizedType = type.Trim();

        return query.Where(p =>
            p.Types.Any(t =>
                t.Equals(
                    normalizedType,
                    StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<PokemonCatalogCacheItem> ApplySizeFilters(
        IEnumerable<PokemonCatalogCacheItem> query,
        PokemonSmartSearchCriteria criteria)
    {
        if (criteria.MinHeight.HasValue)
        {
            query = query.Where(p =>
                p.Height >= criteria.MinHeight.Value);
        }

        if (criteria.MaxHeight.HasValue)
        {
            query = query.Where(p =>
                p.Height <= criteria.MaxHeight.Value);
        }

        if (criteria.MinWeight.HasValue)
        {
            query = query.Where(p =>
                p.Weight >= criteria.MinWeight.Value);
        }

        if (criteria.MaxWeight.HasValue)
        {
            query = query.Where(p =>
                p.Weight <= criteria.MaxWeight.Value);
        }

        return query;
    }
    private static IEnumerable<PokemonCatalogCacheItem> ApplySorting(
        IEnumerable<PokemonCatalogCacheItem> query,
        string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "hp" => query
                .OrderByDescending(p => p.Hp)
                .ThenBy(p => p.PokeApiId),

            "attack" => query
                .OrderByDescending(p => p.Attack)
                .ThenBy(p => p.PokeApiId),

            "defense" => query
                .OrderByDescending(p => p.Defense)
                .ThenBy(p => p.PokeApiId),

            "speed" => query
                .OrderByDescending(p => p.Speed)
                .ThenBy(p => p.PokeApiId),

            "name" => query
                .OrderBy(p => p.Name)
                .ThenBy(p => p.PokeApiId),

            null or "" => query
                .OrderBy(p => p.PokeApiId),

            _ => query
                .OrderBy(p => p.PokeApiId)
        };
    }
    private static IEnumerable<PokemonCatalogCacheItem> ApplySorting(
        IEnumerable<PokemonCatalogCacheItem> query,
        PokemonSmartSearchCriteria criteria)
    {
        var descending =
            criteria.SortDirection.Equals(
                "desc",
                StringComparison.OrdinalIgnoreCase);

        return criteria.SortBy?.ToLowerInvariant() switch
        {
            "hp" => descending
                ? query.OrderByDescending(p => p.Hp)
                : query.OrderBy(p => p.Hp),

            "attack" => descending
                ? query.OrderByDescending(p => p.Attack)
                : query.OrderBy(p => p.Attack),

            "defense" => descending
                ? query.OrderByDescending(p => p.Defense)
                : query.OrderBy(p => p.Defense),

            "specialattack" => descending
                ? query.OrderByDescending(p => p.SpecialAttack)
                : query.OrderBy(p => p.SpecialAttack),

            "specialdefense" => descending
                ? query.OrderByDescending(p => p.SpecialDefense)
                : query.OrderBy(p => p.SpecialDefense),

            "speed" => descending
                ? query.OrderByDescending(p => p.Speed)
                : query.OrderBy(p => p.Speed),

            "height" => descending
                ? query.OrderByDescending(p => p.Height)
                : query.OrderBy(p => p.Height),

            "weight" => descending
                ? query.OrderByDescending(p => p.Weight)
                : query.OrderBy(p => p.Weight),

            "baseexperience" => descending
                ? query.OrderByDescending(p => p.BaseExperience ?? 0)
                : query.OrderBy(p => p.BaseExperience ?? 0),

            "totalstats" => descending
                ? query.OrderByDescending(GetTotalStats)
                : query.OrderBy(GetTotalStats),

            _ => query.OrderBy(p => p.PokeApiId)
        };
    }

    private static IEnumerable<PokemonCatalogCacheItem> ApplyRequestedCount(
        IEnumerable<PokemonCatalogCacheItem> query,
        PokemonSmartSearchCriteria criteria)
    {
        if (!criteria.RequestedCount.HasValue)
        {
            return query;
        }

        var requestedCount = Math.Clamp(
            criteria.RequestedCount.Value,
            1,
            100);

        return query.Take(requestedCount);
    }

    private static PokemonPagedResponse BuildPagedResponseFromCache(
        IEnumerable<PokemonCatalogCacheItem> query,
        int page,
        int pageSize)
    {
        var list = query.ToList();

        var totalCount = list.Count;

        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapCacheItemToListItem)
            .ToList();

        return new PokemonPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };
    }

    private static PokemonListItemResponse MapCacheItemToListItem(
        PokemonCatalogCacheItem item)
    {
        return new PokemonListItemResponse
        {
            Id = item.Id,
            PokeApiId = item.PokeApiId,
            Name = item.Name,
            ImageUrl = item.ImageUrl,
            Height = item.Height,
            Weight = item.Weight,
            BaseExperience = item.BaseExperience,
            Hp = item.Hp,
            Attack = item.Attack,
            Defense = item.Defense,
            SpecialAttack = item.SpecialAttack,
            SpecialDefense = item.SpecialDefense,
            Speed = item.Speed,
            IsLegendary = item.IsLegendary,
            Types = item.Types
        };
    }

    private static PokemonDetailsResponse MapCacheItemToDetails(
        PokemonCatalogCacheItem item)
    {
        var stats = new List<PokemonStatResponse>
        {
            new() { Name = "hp", BaseStat = item.Hp },
            new() { Name = "attack", BaseStat = item.Attack },
            new() { Name = "defense", BaseStat = item.Defense },
            new() { Name = "special-attack", BaseStat = item.SpecialAttack },
            new() { Name = "special-defense", BaseStat = item.SpecialDefense },
            new() { Name = "speed", BaseStat = item.Speed }
        };

        return new PokemonDetailsResponse
        {
            Id = item.Id,
            PokeApiId = item.PokeApiId,
            Name = item.Name,
            ImageUrl = item.ImageUrl,
            Height = item.Height,
            Weight = item.Weight,
            BaseExperience = item.BaseExperience,
            IsLegendary = item.IsLegendary,
            CreatedAt = DateTime.UtcNow,
            Types = item.Types,
            Stats = stats,
            Abilities = new()
        };
    }

    private static int GetTotalStats(
        PokemonCatalogCacheItem item)
    {
        return item.Hp +
               item.Attack +
               item.Defense +
               item.SpecialAttack +
               item.SpecialDefense +
               item.Speed;
    }

}