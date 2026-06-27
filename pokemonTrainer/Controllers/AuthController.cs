using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pokemonTrainer.DTOs.Auth;
using pokemonTrainer.DTOs.Common;
using pokemonTrainer.Services;

namespace pokemonTrainer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return Ok(result.Data);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Email = User.FindFirstValue(ClaimTypes.Email),
            Name = User.FindFirstValue(ClaimTypes.Name)
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
            "EMAIL_ALREADY_EXISTS" => BadRequest(error),
            "REGISTRATION_FAILED" => BadRequest(error),
            "INVALID_CREDENTIALS" => Unauthorized(error),
            "LOGIN_UPDATE_FAILED" => BadRequest(error),
            _ => BadRequest(error)
        };
    }
}