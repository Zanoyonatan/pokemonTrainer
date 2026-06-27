using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pokemonTrainer.DTOs.Common;
using pokemonTrainer.DTOs.DreamTeam;
using pokemonTrainer.Services;

namespace pokemonTrainer.Controllers;

[ApiController]
[Authorize]
[Route("api/dream-team")]
public class DreamTeamController : ControllerBase
{
    private readonly DreamTeamService _dreamTeamService;
    private readonly PokemonImportStatusService _statusService;

    public DreamTeamController(
        DreamTeamService dreamTeamService,
        PokemonImportStatusService statusService)
    {
        _dreamTeamService = dreamTeamService;
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyDreamTeam(
        CancellationToken cancellationToken = default)
    {
        if (!_statusService.IsReady)
        {
            return PokemonDataNotReady();
        }

        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var response = await _dreamTeamService.GetMyTeamAsync(
            userId,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> AddPokemon(
        AddDreamTeamPokemonRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_statusService.IsReady)
        {
            return PokemonDataNotReady();
        }

        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _dreamTeamService.AddPokemonAsync(
            userId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetMyDreamTeam),
            result.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePokemon(
        int id,
        UpdateDreamTeamPokemonRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _dreamTeamService.UpdateNicknameAsync(
            userId,
            id,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> RemovePokemon(
        int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _dreamTeamService.RemovePokemonAsync(
            userId,
            id,
            cancellationToken);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return NoContent();
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
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
            "DREAM_TEAM_POKEMON_NOT_FOUND" => NotFound(error),
            "TEAM_FULL" => BadRequest(error),
            "POKEMON_ALREADY_EXISTS" => BadRequest(error),
            _ => BadRequest(error)
        };
    }
}