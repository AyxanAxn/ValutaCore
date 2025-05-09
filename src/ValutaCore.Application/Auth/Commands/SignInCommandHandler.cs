namespace ValutaCore.Application.Auth.Commands;

public class SignInCommandHandler(
    IJwtTokenService jwtTokenService,
    IOptions<CredentialSettings> credOptions,
    ILogger<SignInCommandHandler> logger)
    : IRequestHandler<SignInCommand, SignInResponse>
{
    private readonly CredentialSettings _cred = credOptions.Value;

    public Task<SignInResponse> Handle(SignInCommand cmd, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var profile = _cred.Users.FirstOrDefault(u =>
            u.Username.Equals(cmd.Username, StringComparison.OrdinalIgnoreCase));

        if (profile == null || profile.Password != cmd.Password)
        {
            sw.Stop();
            logger.LogWarning(
                "Failed login attempt for {User} from {Ip} in {Elapsed}ms",
                cmd.Username, cmd.ClientIp, sw.ElapsedMilliseconds);

            // Throwing causes ApiController to turn it into 401
            throw new AuthenticationException("Invalid credentials");
        }

        var clientId = Guid.NewGuid().ToString();
        var token = jwtTokenService.GenerateToken(
            cmd.Username,
            clientId,
            profile.Roles);

        sw.Stop();
        logger.LogInformation(
            "Successful login for {User} from {Ip} | ClientId: {ClientId} | Time: {Elapsed}ms",
            cmd.Username, cmd.ClientIp, clientId, sw.ElapsedMilliseconds);

        return Task.FromResult(new SignInResponse
        {
            Username = cmd.Username,
            Token = token,
            Roles = profile.Roles
        });
    }
}
