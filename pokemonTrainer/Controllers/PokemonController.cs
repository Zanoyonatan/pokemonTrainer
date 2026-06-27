using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
using pokemonTrainer.DTOs.Pokemon;
using pokemonTrainer.Models;
using pokemonTrainer.Services;

namespace pokemonTrainer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PokemonController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly PokemonImportStatusService _statusService;

    public PokemonController(
        ApplicationDbContext dbContext,
        PokemonImportStatusService statusService)
    {
        _dbContext = dbContext;
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_statusService.IsReady)
        {
            return PokemonDataNotReady();
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Pokemons
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();

            if (int.TryParse(normalizedSearch, out var pokeApiId))
            {
                query = query.Where(p =>
                    p.PokeApiId == pokeApiId ||
                    EF.Functions.Like(p.Name, $"%{normalizedSearch}%"));
            }
            else
            {
                query = query.Where(p =>
                    EF.Functions.Like(p.Name, $"%{normalizedSearch}%"));
            }
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim().ToLowerInvariant();

            query = query.Where(p =>
                p.PokemonTypes.Any(pt =>
                    pt.PokemonType.Name == normalizedType));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pokemons = await query
            .OrderBy(p => p.PokeApiId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.PokemonTypes)
            .ThenInclude(pt => pt.PokemonType)
            .ToListAsync(cancellationToken);

        var response = new PokemonPagedResponse
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

        return Ok(response);
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetTypes(
        CancellationToken cancellationToken = default)
    {
        if (!_statusService.IsReady)
        {
            return PokemonDataNotReady();
        }

        var types = await _dbContext.PokemonTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToListAsync(cancellationToken);

        return Ok(types);
    }

    [HttpGet("{pokeApiId:int}")]
    public async Task<IActionResult> GetByPokeApiId(
        int pokeApiId,
        CancellationToken cancellationToken = default)
    {
        if (!_statusService.IsReady)
        {
            return PokemonDataNotReady();
        }

        var pokemon = await _dbContext.Pokemons
            .AsNoTracking()
            .Include(p => p.PokemonTypes)
            .ThenInclude(pt => pt.PokemonType)
            .FirstOrDefaultAsync(
                p => p.PokeApiId == pokeApiId,
                cancellationToken);

        if (pokemon == null)
        {
            return NotFound(new
            {
                Message = $"Pokémon with PokeApiId {pokeApiId} was not found."
            });
        }

        return Ok(MapToDetails(pokemon));
    }

    private IActionResult PokemonDataNotReady()
    {
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            Status = "ImportInProgress",
            Message = "Pokémon data is still loading. Please try again shortly."
        });
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