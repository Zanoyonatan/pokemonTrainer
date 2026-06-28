using Microsoft.AspNetCore.Mvc;
using pokemonTrainer.Services;

namespace pokemonTrainer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly DatabaseAvailabilityService _dbAvailabilityService;

    public HealthController(DatabaseAvailabilityService dbAvailabilityService)
    {
        _dbAvailabilityService = dbAvailabilityService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Pokemon Trainer API",
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("db")]
    public async Task<IActionResult> GetDb(CancellationToken cancellationToken = default)
    {
        try
        {
            var isAvailable = await _dbAvailabilityService.IsDatabaseAvailableAsync(cancellationToken);

            if (isAvailable)
            {
                return Ok(new
                {
                    Status = "Healthy",
                    Database = "Available"
                });
            }
            else
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        Status = "Unhealthy",
                        Database = "Unavailable"
                    });
            }
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    Status = "Unhealthy",
                    Database = "Unavailable"
                });
        }
    }
}