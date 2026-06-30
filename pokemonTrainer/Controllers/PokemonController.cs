using Microsoft.AspNetCore.Mvc;
using pokemonTrainer.DTOs.Common;
using pokemonTrainer.DTOs.Nicknames;
using pokemonTrainer.Services;

namespace pokemonTrainer.Controllers;

using Microsoft.AspNetCore.Authorization;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PokemonController : ControllerBase
{
    private readonly PokemonService _pokemonService;
    private readonly PokemonNicknameService _nicknameService;
    private readonly PokemonImportStatusService _statusService;

    public PokemonController(
        PokemonService pokemonService,
        PokemonNicknameService nicknameService,
        PokemonImportStatusService statusService)
    {
        _pokemonService = pokemonService;
        _nicknameService = nicknameService;
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        if (!_statusService.IsReady)
        {
            return PokemonDataNotReady();
        }

        var response = await _pokemonService.GetPagedAsync(
            search,
            type,
            page,
            pageSize,
            sortBy,
            cancellationToken);

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

        var response = await _pokemonService.GetTypesAsync(
            cancellationToken);

        return Ok(response);
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

        var result = await _pokemonService.GetByPokeApiIdAsync(
            pokeApiId,
            cancellationToken);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return Ok(result.Data);
    }

    [HttpPost("{pokeApiId:int}/generate-nicknames")]
    public async Task<IActionResult> GenerateNicknames(
        int pokeApiId,
        GeneratePokemonNicknamesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_statusService.IsReady)
        {
            return PokemonDataNotReady();
        }

        var result = await _nicknameService.GenerateAsync(
            pokeApiId,
            request.Count,
            cancellationToken);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return Ok(result.Data);
    }

    private IActionResult PokemonDataNotReady()
    {
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            Status = "ImportInProgress",
            Message = "Pokémon data is still loading. Please try again shortly."
        });
    }

    private IActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        var error = new
        {
            result.ErrorCode,
            result.Message
        };

        return result.ErrorCode switch
        {
            "POKEMON_NOT_FOUND" => NotFound(error),
            _ => BadRequest(error)
        };
    }
}