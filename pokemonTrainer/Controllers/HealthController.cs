using Microsoft.AspNetCore.Mvc;

namespace pokemonTrainer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
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
}