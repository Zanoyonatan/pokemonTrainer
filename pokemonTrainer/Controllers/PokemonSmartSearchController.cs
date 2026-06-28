using Microsoft.AspNetCore.Mvc;
using pokemonTrainer.DTOs.AiSearch;
using pokemonTrainer.Services;

namespace pokemonTrainer.Controllers;

[ApiController]
[Route("api/pokemon-smart-search")]
public class PokemonSmartSearchController : ControllerBase
{
    private readonly PokemonSmartSearchService _smartSearchService;
    private readonly PokemonImportStatusService _statusService;

    public PokemonSmartSearchController(
        PokemonSmartSearchService smartSearchService,
        PokemonImportStatusService statusService)
    {
        _smartSearchService = smartSearchService;
        _statusService = statusService;
    }

    [HttpPost]
    public async Task<IActionResult> Search(
        PokemonSmartSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_statusService.IsReady)
        {
            return PokemonDataNotReady();
        }

        var response = await _smartSearchService.SearchAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    private IActionResult PokemonDataNotReady()
    {
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            Status = "ImportInProgress",
            Message = "Pokémon data is still loading. Please try again shortly."
        });
    }
}