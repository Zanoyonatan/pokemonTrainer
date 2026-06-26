namespace pokemonTrainer.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public UserInfoResponse User { get; set; } = new();
}