namespace ValutaCore.Application.Auth.DTOs;

public class SignInResponse
{
    public string Username { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = [];
}