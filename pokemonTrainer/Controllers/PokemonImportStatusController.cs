using Microsoft.AspNetCore.Mvc;
using pokemonTrainer.Services;

namespace pokemonTrainer.Controllers;

[ApiController]
[Route("api/pokemon-import-status")]
public class PokemonImportStatusController : ControllerBase
{
    private readonly PokemonImportStatusService _statusService;

    public PokemonImportStatusController(
        PokemonImportStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_statusService.GetStatus());
    }
}