namespace ValutaCore.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthenticationController(
    IJwtTokenService jwtTokenService,
    ILogger<AuthenticationController> logger,
    IOptions<CredentialSettings> userCredentialsOptions)
    : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService =
        jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));

    private readonly ILogger<AuthenticationController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly CredentialSettings _credentialSettings =
        userCredentialsOptions?.Value ?? throw new ArgumentNullException(nameof(userCredentialsOptions));

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] SignInRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest("Username and password are required");

        var profile = _credentialSettings.Users
            .FirstOrDefault(u => 
                u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));


        if (profile == null || profile.Password != request.Password)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Failed login attempt for {UserHandle} from {ClientIp} in {Elapsed}ms",
                request.Username, clientIp, stopwatch.ElapsedMilliseconds);

            return Unauthorized("Invalid username or password");
        }

        var clientId = Guid.NewGuid().ToString();
        var token = _jwtTokenService.GenerateToken(
            request.Username,
            clientId,
            profile.Roles);

        stopwatch.Stop();
        _logger.LogInformation(
            "Successful login for {UserHandle} from {ClientIp} | ClientId: {ClientId} | Time: {Elapsed}ms",
            request.Username, clientIp, clientId, stopwatch.ElapsedMilliseconds);

        return Ok(new SignInResponse
        {
            Username = request.Username,
            Token = token,
            Roles = profile.Roles
        });
    }
}