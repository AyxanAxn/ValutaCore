namespace ValutaCore.Application.Auth.Commands;

public record SignInCommand(string Username, string Password, string ClientIp)
    : IRequest<SignInResponse>;