namespace ValutaCore.Infrastructure.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string clientId, IEnumerable<string> roles);
}