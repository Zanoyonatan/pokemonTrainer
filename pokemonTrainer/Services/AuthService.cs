using Microsoft.AspNetCore.Identity;
using pokemonTrainer.DTOs.Auth;
using pokemonTrainer.DTOs.Common;
using pokemonTrainer.Models;

namespace pokemonTrainer.Services;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<ServiceResult<UserInfoResponse>> RegisterAsync(
        RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            return ServiceResult<UserInfoResponse>.Fail(
                "EMAIL_ALREADY_EXISTS",
                "Email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            var message = string.Join(
                " ",
                result.Errors.Select(e => e.Description));

            return ServiceResult<UserInfoResponse>.Fail(
                "REGISTRATION_FAILED",
                message);
        }

        return ServiceResult<UserInfoResponse>.Ok(
            MapToUserInfo(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return ServiceResult<AuthResponse>.Fail(
                "INVALID_CREDENTIALS",
                "Invalid email or password.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(
            user,
            request.Password);

        if (!passwordValid)
        {
            return ServiceResult<AuthResponse>.Fail(
                "INVALID_CREDENTIALS",
                "Invalid email or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var message = string.Join(
                " ",
                updateResult.Errors.Select(e => e.Description));

            return ServiceResult<AuthResponse>.Fail(
                "LOGIN_UPDATE_FAILED",
                message);
        }

        var tokenResult = _jwtTokenService.CreateToken(user);

        var response = new AuthResponse
        {
            Token = tokenResult.Token,
            ExpiresAt = tokenResult.ExpiresAt,
            User = MapToUserInfo(user)
        };

        return ServiceResult<AuthResponse>.Ok(response);
    }

    private static UserInfoResponse MapToUserInfo(ApplicationUser user)
    {
        return new UserInfoResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName
        };
    }
}