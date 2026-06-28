using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
using pokemonTrainer.DTOs.AiSearch;
using pokemonTrainer.DTOs.Common;
using pokemonTrainer.DTOs.Pokemon;
using pokemonTrainer.Models;

namespace pokemonTrainer.Services;

public class PokemonService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly PokemonCatalogCacheService _cacheService;

    public PokemonService(
        ApplicationDbContext dbContext,
        PokemonCatalogCacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async Task<PokemonPagedResponse> GetPagedAsync(
        string? search,
        string? type,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        try
        {
            var query = _dbContext.Pokemons
                .AsNoTracking()
                .AsQueryable();

            query = ApplySearchFilter(query, search);
            query = ApplyTypeFilter(query, type);
            query = query.OrderBy(p => p.PokeApiId);

            return await BuildPagedResponseAsync(
                query,
                page,
                pageSize,
                cancellationToken);
        }
        catch (Exception ex) when (DatabaseAvailabilityService.IsDatabaseUnavailableException(ex))
        {
            // Fall back to cache
            return GetPagedFromCacheAsync(search, type, page, pageSize);
        }
    }

    public async Task<PokemonPagedResponse> SearchByCriteriaAsync(
        PokemonSmartSearchCriteria criteria,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        try
        {
            var query = _dbContext.Pokemons
                .AsNoTracking()
                .AsQueryable();

            query = ApplySearchFilter(query, criteria.NameSearch);
            query = ApplyTypeFilter(query, criteria.Type);
            query = ApplySizeFilters(query, criteria);
            query = ApplySorting(query, criteria);
            query = ApplyRequestedCount(query, criteria);

            return await BuildPagedResponseAsync(
                query,
                page,
                pageSize,
                cancellationToken);
        }
        catch (Exception ex) when (DatabaseAvailabilityService.IsDatabaseUnavailableException(ex))
        {
            // Fall back to cache
            return SearchByCriteriaFromCacheAsync(criteria, page, pageSize);
        }
    }

    public async Task<List<string>> GetTypesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.PokemonTypes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => t.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (DatabaseAvailabilityService.IsDatabaseUnavailableException(ex))
        {
            // Fall back to cache
            var cached = _cacheService.GetCachedCatalog();
            if (cached != null)
            {
                return cached
                    .SelectMany(p => p.Types)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();
            }
            throw;
        }
    }

    public async Task<ServiceResult<PokemonDetailsResponse>> GetByPokeApiIdAsync(
        int pokeApiId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pokemon = await _dbContext.Pokemons
                .AsNoTracking()
                .Include(p => p.PokemonTypes)
                    .ThenInclude(pt => pt.PokemonType)
                .FirstOrDefaultAsync(
                    p => p.PokeApiId == pokeApiId,
                    cancellationToken);

            if (pokemon == null)
            {
                return ServiceResult<PokemonDetailsResponse>.Fail(
                    "POKEMON_NOT_FOUND",
                    $"Pokémon with PokeApiId {pokeApiId} was not found.");
            }

            return ServiceResult<PokemonDetailsResponse>.Ok(
                MapToDetails(pokemon));
        }
        catch (Exception ex) when (DatabaseAvailabilityService.IsDatabaseUnavailableException(ex))
        {
            // Fall back to cache
            var cached = _cacheService.FindInCache(pokeApiId);
            if (cached != null)
            {
                return ServiceResult<PokemonDetailsResponse>.Ok(
                    MapCacheItemToDetails(cached));
            }

            return ServiceResult<PokemonDetailsResponse>.Fail(
                "POKEMON_NOT_FOUND",
                $"Pokémon with PokeApiId {pokeApiId} was not found.");
        }
    }

    private PokemonPagedResponse GetPagedFromCacheAsync(
        string? search,
        string? type,
        int page,
        int pageSize)
    {
        var cached = _cacheService.GetCachedCatalog();
        if (cached == null)
        {
            throw new InvalidOperationException(
                "The database is currently unavailable and no cached Pokémon catalog is available.");
        }

        // Apply filters to cached data
        var filtered = cached.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            if (int.TryParse(normalizedSearch, out var pokeApiId))
            {
                filtered = filtered.Where(p =>
                    p.PokeApiId == pokeApiId ||
                    p.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                filtered = filtered.Where(p =>
                    p.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim().ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Types.Any(t => t.Equals(normalizedType, StringComparison.OrdinalIgnoreCase)));
        }

        filtered = filtered.OrderBy(p => p.PokeApiId);

        var totalCount = filtered.Count();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapCacheItemToListItem)
            .ToList();

        return new PokemonPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };
    }

    private PokemonPagedResponse SearchByCriteriaFromCacheAsync(
        PokemonSmartSearchCriteria criteria,
        int page,
        int pageSize)
    {
        var cached = _cacheService.GetCachedCatalog();
        if (cached == null)
        {
            throw new InvalidOperationException(
                "The database is currently unavailable and no cached Pokémon catalog is available.");
        }

        var filtered = cached.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(criteria.NameSearch))
        {
            var normalizedSearch = criteria.NameSearch.Trim().ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        }

        // Apply type filter
        if (!string.IsNullOrWhiteSpace(criteria.Type))
        {
            var normalizedType = criteria.Type.Trim().ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Types.Any(t => t.Equals(normalizedType, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply size filters
        if (criteria.MinHeight.HasValue)
            filtered = filtered.Where(p => p.Height >= criteria.MinHeight.Value);
        if (criteria.MaxHeight.HasValue)
            filtered = filtered.Where(p => p.Height <= criteria.MaxHeight.Value);
        if (criteria.MinWeight.HasValue)
            filtered = filtered.Where(p => p.Weight >= criteria.MinWeight.Value);
        if (criteria.MaxWeight.HasValue)
            filtered = filtered.Where(p => p.Weight <= criteria.MaxWeight.Value);

        // Apply sorting
        filtered = ApplyCacheSorting(filtered, criteria);

        // Apply requested count
        if (criteria.RequestedCount.HasValue)
        {
            var requestedCount = Math.Clamp(criteria.RequestedCount.Value, 1, 100);
            filtered = filtered.Take(requestedCount);
        }

        var totalCount = filtered.Count();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapCacheItemToListItem)
            .ToList();

        return new PokemonPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };
    }

    private static IEnumerable<PokemonCatalogCacheItem> ApplyCacheSorting(
        IEnumerable<PokemonCatalogCacheItem> items,
        PokemonSmartSearchCriteria criteria)
    {
        var descending = criteria.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return criteria.SortBy?.ToLowerInvariant() switch
        {
            "hp" => descending ? items.OrderByDescending(p => p.Hp) : items.OrderBy(p => p.Hp),
            "attack" => descending ? items.OrderByDescending(p => p.Attack) : items.OrderBy(p => p.Attack),
            "defense" => descending ? items.OrderByDescending(p => p.Defense) : items.OrderBy(p => p.Defense),
            "specialattack" => descending ? items.OrderByDescending(p => p.SpecialAttack) : items.OrderBy(p => p.SpecialAttack),
            "specialdefense" => descending ? items.OrderByDescending(p => p.SpecialDefense) : items.OrderBy(p => p.SpecialDefense),
            "speed" => descending ? items.OrderByDescending(p => p.Speed) : items.OrderBy(p => p.Speed),
            "height" => descending ? items.OrderByDescending(p => p.Height) : items.OrderBy(p => p.Height),
            "weight" => descending ? items.OrderByDescending(p => p.Weight) : items.OrderBy(p => p.Weight),
            "baseexperience" => descending ? items.OrderByDescending(p => p.BaseExperience ?? 0) : items.OrderBy(p => p.BaseExperience ?? 0),
            "totalstats" => descending
                ? items.OrderByDescending(p => p.Hp + p.Attack + p.Defense + p.SpecialAttack + p.SpecialDefense + p.Speed)
                : items.OrderBy(p => p.Hp + p.Attack + p.Defense + p.SpecialAttack + p.SpecialDefense + p.Speed),
            _ => items.OrderBy(p => p.PokeApiId)
        };
    }

    private async Task<PokemonPagedResponse> BuildPagedResponseAsync(
        IQueryable<Pokemon> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var pokemons = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.PokemonTypes)
                .ThenInclude(pt => pt.PokemonType)
            .ToListAsync(cancellationToken);

        // Refresh cache after successful DB read
        var allPokemons = await query
            .Include(p => p.PokemonTypes)
                .ThenInclude(pt => pt.PokemonType)
            .ToListAsync(cancellationToken);

        var cacheItems = allPokemons
            .Select(MapToCacheItem)
            .ToList();

        if (cacheItems.Count > 0)
        {
            _cacheService.RefreshCache(cacheItems);
        }

        return new PokemonPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = pokemons
                .Select(MapToListItem)
                .ToList()
        };
    }

    private static IQueryable<Pokemon> ApplySearchFilter(
        IQueryable<Pokemon> query,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var normalizedSearch = search.Trim().ToLowerInvariant();

        if (int.TryParse(normalizedSearch, out var pokeApiId))
        {
            return query.Where(p =>
                p.PokeApiId == pokeApiId ||
                EF.Functions.Like(p.Name, $"%{normalizedSearch}%"));
        }

        return query.Where(p =>
            EF.Functions.Like(p.Name, $"%{normalizedSearch}%"));
    }

    private static IQueryable<Pokemon> ApplyTypeFilter(
        IQueryable<Pokemon> query,
        string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return query;
        }

        var normalizedType = type.Trim().ToLowerInvariant();

        return query.Where(p =>
            p.PokemonTypes.Any(pt =>
                pt.PokemonType.Name == normalizedType));
    }

    private static IQueryable<Pokemon> ApplySizeFilters(
        IQueryable<Pokemon> query,
        PokemonSmartSearchCriteria criteria)
    {
        if (criteria.MinHeight.HasValue)
        {
            query = query.Where(p => p.Height >= criteria.MinHeight.Value);
        }

        if (criteria.MaxHeight.HasValue)
        {
            query = query.Where(p => p.Height <= criteria.MaxHeight.Value);
        }

        if (criteria.MinWeight.HasValue)
        {
            query = query.Where(p => p.Weight >= criteria.MinWeight.Value);
        }

        if (criteria.MaxWeight.HasValue)
        {
            query = query.Where(p => p.Weight <= criteria.MaxWeight.Value);
        }

        return query;
    }

    private static IQueryable<Pokemon> ApplySorting(
        IQueryable<Pokemon> query,
        PokemonSmartSearchCriteria criteria)
    {
        var descending =
            criteria.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

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
                ? query.OrderByDescending(p => p.BaseExperience)
                : query.OrderBy(p => p.BaseExperience),

            "totalstats" => descending
                ? query.OrderByDescending(p =>
                    p.Hp +
                    p.Attack +
                    p.Defense +
                    p.SpecialAttack +
                    p.SpecialDefense +
                    p.Speed)
                : query.OrderBy(p =>
                    p.Hp +
                    p.Attack +
                    p.Defense +
                    p.SpecialAttack +
                    p.SpecialDefense +
                    p.Speed),

            _ => query.OrderBy(p => p.PokeApiId)
        };
    }

    private static IQueryable<Pokemon> ApplyRequestedCount(
        IQueryable<Pokemon> query,
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

    private static PokemonListItemResponse MapToListItem(Pokemon pokemon)
    {
        return new PokemonListItemResponse
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

    private static PokemonListItemResponse MapCacheItemToListItem(PokemonCatalogCacheItem item)
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

    private static PokemonDetailsResponse MapToDetails(Pokemon pokemon)
    {
        var stats = new List<PokemonStatResponse>
        {
            new() { Name = "hp", BaseStat = pokemon.Hp },
            new() { Name = "attack", BaseStat = pokemon.Attack },
            new() { Name = "defense", BaseStat = pokemon.Defense },
            new() { Name = "special-attack", BaseStat = pokemon.SpecialAttack },
            new() { Name = "special-defense", BaseStat = pokemon.SpecialDefense },
            new() { Name = "speed", BaseStat = pokemon.Speed }
        };

        var abilities = ParseAbilities(pokemon.AbilitiesJson);

        return new PokemonDetailsResponse
        {
            Id = pokemon.Id,
            PokeApiId = pokemon.PokeApiId,
            Name = pokemon.Name,
            ImageUrl = pokemon.ImageUrl,
            Height = pokemon.Height,
            Weight = pokemon.Weight,
            BaseExperience = pokemon.BaseExperience,
            IsLegendary = pokemon.IsLegendary,
            CreatedAt = pokemon.CreatedAt,
            Types = pokemon.PokemonTypes
                .Select(pt => pt.PokemonType.Name)
                .ToList(),
            Stats = stats,
            Abilities = abilities
        };
    }

    private static PokemonDetailsResponse MapCacheItemToDetails(PokemonCatalogCacheItem item)
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

    private static List<PokemonAbilityResponse> ParseAbilities(string? abilitiesJson)
    {
        if (string.IsNullOrEmpty(abilitiesJson))
        {
            return new();
        }

        try
        {
            var abilities = JsonSerializer.Deserialize<List<PokemonAbilityResponse>>(
                abilitiesJson,
                JsonOptions);

            return abilities ?? new();
        }
        catch
        {
            return new();
        }
    }
}