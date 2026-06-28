using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Data;
using pokemonTrainer.DTOs.Common;
using pokemonTrainer.DTOs.DreamTeam;
using pokemonTrainer.Models;

namespace pokemonTrainer.Services;

public class DreamTeamService
{
    private const int MaxTeamSize = 5;

    private readonly ApplicationDbContext _dbContext;

    public DreamTeamService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DreamTeamResponse> GetMyTeamAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.DreamTeamPokemons
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Include(d => d.Pokemon)
                .ThenInclude(p => p.PokemonTypes)
                    .ThenInclude(pt => pt.PokemonType)
            .OrderBy(d => d.Slot)
            .ToListAsync(cancellationToken);

        return new DreamTeamResponse
        {
            MaxSize = MaxTeamSize,
            CurrentSize = items.Count,
            Items = items.Select(MapToResponse).ToList()
        };
    }

    public async Task<ServiceResult<DreamTeamPokemonResponse>> AddPokemonAsync(
        string userId,
        AddDreamTeamPokemonRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentTeam = await _dbContext.DreamTeamPokemons
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);

        if (currentTeam.Count >= MaxTeamSize)
        {
            return ServiceResult<DreamTeamPokemonResponse>.Fail(
                "TEAM_FULL",
                $"Dream Team can contain up to {MaxTeamSize} Pokémon.");
        }

        var pokemon = await _dbContext.Pokemons
            .Include(p => p.PokemonTypes)
                .ThenInclude(pt => pt.PokemonType)
            .FirstOrDefaultAsync(
                p => p.PokeApiId == request.PokeApiId,
                cancellationToken);

        if (pokemon == null)
        {
            return ServiceResult<DreamTeamPokemonResponse>.Fail(
                "POKEMON_NOT_FOUND",
                $"Pokémon with PokeApiId {request.PokeApiId} was not found.");
        }

        var alreadyExists = currentTeam.Any(d =>
            d.PokemonId == pokemon.Id);

        if (alreadyExists)
        {
            return ServiceResult<DreamTeamPokemonResponse>.Fail(
                "POKEMON_ALREADY_EXISTS",
                "This Pokémon already exists in your Dream Team.");
        }

        var nextSlot = GetNextAvailableSlot(currentTeam);

        var dreamTeamPokemon = new DreamTeamPokemon
        {
            UserId = userId,
            PokemonId = pokemon.Id,
            Slot = nextSlot,
            Nickname = NormalizeNickname(request.Nickname)
        };

        _dbContext.DreamTeamPokemons.Add(dreamTeamPokemon);

        await _dbContext.SaveChangesAsync(cancellationToken);

        dreamTeamPokemon.Pokemon = pokemon;

        return ServiceResult<DreamTeamPokemonResponse>.Ok(
            MapToResponse(dreamTeamPokemon));
    }

    public async Task<ServiceResult<DreamTeamPokemonResponse>> UpdateNicknameAsync(
        string userId,
        int dreamTeamPokemonId,
        UpdateDreamTeamPokemonRequest request,
        CancellationToken cancellationToken = default)
    {
        var dreamTeamPokemon = await _dbContext.DreamTeamPokemons
            .Include(d => d.Pokemon)
                .ThenInclude(p => p.PokemonTypes)
                    .ThenInclude(pt => pt.PokemonType)
            .FirstOrDefaultAsync(
                d => d.Id == dreamTeamPokemonId && d.UserId == userId,
                cancellationToken);

        if (dreamTeamPokemon == null)
        {
            return ServiceResult<DreamTeamPokemonResponse>.Fail(
                "DREAM_TEAM_POKEMON_NOT_FOUND",
                "Dream Team Pokémon was not found.");
        }

        dreamTeamPokemon.Nickname = NormalizeNickname(request.Nickname);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<DreamTeamPokemonResponse>.Ok(
            MapToResponse(dreamTeamPokemon));
    }

    public async Task<ServiceResult<bool>> RemovePokemonAsync(
        string userId,
        int dreamTeamPokemonId,
        CancellationToken cancellationToken = default)
    {
        var dreamTeamPokemon = await _dbContext.DreamTeamPokemons
            .FirstOrDefaultAsync(
                d => d.Id == dreamTeamPokemonId && d.UserId == userId,
                cancellationToken);

        if (dreamTeamPokemon == null)
        {
            return ServiceResult<bool>.Fail(
                "DREAM_TEAM_POKEMON_NOT_FOUND",
                "Dream Team Pokémon was not found.");
        }

        _dbContext.DreamTeamPokemons.Remove(dreamTeamPokemon);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Ok(true);
    }

    private static int GetNextAvailableSlot(
        List<DreamTeamPokemon> currentTeam)
    {
        var usedSlots = currentTeam
            .Select(d => d.Slot)
            .ToHashSet();

        return Enumerable.Range(1, MaxTeamSize)
            .First(slot => !usedSlots.Contains(slot));
    }

    private static string? NormalizeNickname(string? nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return null;
        }

        return nickname.Trim();
    }

    private static DreamTeamPokemonResponse MapToResponse(
        DreamTeamPokemon dreamTeamPokemon)
    {
        var pokemon = dreamTeamPokemon.Pokemon;

        return new DreamTeamPokemonResponse
        {
            Id = dreamTeamPokemon.Id,
            Slot = dreamTeamPokemon.Slot,
            Nickname = dreamTeamPokemon.Nickname,

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
}