using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
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
}