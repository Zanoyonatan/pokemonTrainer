using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
using pokemonTrainer.DTOs.Common;
using pokemonTrainer.DTOs.Pokemon;
using pokemonTrainer.Models;
using pokemonTrainer.DTOs.AiSearch;

namespace pokemonTrainer.Services;

public class PokemonService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApplicationDbContext _dbContext;

    public PokemonService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var query = _dbContext.Pokemons
            .AsNoTracking()
            .AsQueryable();

        query = ApplySearchFilter(query, search);
        query = ApplyTypeFilter(query, type);

        var totalCount = await query.CountAsync(cancellationToken);

        var pokemons = await query
            .OrderBy(p => p.PokeApiId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.PokemonTypes)
                .ThenInclude(pt => pt.PokemonType)
            .ToListAsync(cancellationToken);

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

    public async Task<List<string>> GetTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PokemonTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<PokemonDetailsResponse>> GetByPokeApiIdAsync(
        int pokeApiId,
        CancellationToken cancellationToken = default)
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
                .OrderBy(name => name)
                .ToList()
        };
    }

    private static PokemonDetailsResponse MapToDetails(Pokemon pokemon)
    {
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
                .OrderBy(name => name)
                .ToList(),
            Stats = ParseStats(pokemon.StatsJson),
            Abilities = ParseAbilities(pokemon.AbilitiesJson)
        };
    }

    private static List<PokemonStatResponse> ParseStats(string? statsJson)
    {
        if (string.IsNullOrWhiteSpace(statsJson))
        {
            return new List<PokemonStatResponse>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<PokemonStatResponse>>(
                       statsJson,
                       JsonOptions)
                   ?? new List<PokemonStatResponse>();
        }
        catch
        {
            return new List<PokemonStatResponse>();
        }
    }

    private static List<PokemonAbilityResponse> ParseAbilities(string? abilitiesJson)
    {
        if (string.IsNullOrWhiteSpace(abilitiesJson))
        {
            return new List<PokemonAbilityResponse>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<PokemonAbilityResponse>>(
                       abilitiesJson,
                       JsonOptions)
                   ?? new List<PokemonAbilityResponse>();
        }
        catch
        {
            return new List<PokemonAbilityResponse>();
        }
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

            _ => query.OrderBy(p => p.PokeApiId)
        };
    }
    public async Task<PokemonPagedResponse> SearchByCriteriaAsync(
    PokemonSmartSearchCriteria criteria,
    int page = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Pokemons
            .AsNoTracking()
            .AsQueryable();

        query = ApplySearchFilter(query, criteria.NameSearch);
        query = ApplyTypeFilter(query, criteria.Type);
        query = ApplySizeFilters(query, criteria);
        query = ApplySorting(query, criteria);

        var totalCount = await query.CountAsync(cancellationToken);

        var pokemons = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.PokemonTypes)
                .ThenInclude(pt => pt.PokemonType)
            .ToListAsync(cancellationToken);

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
}