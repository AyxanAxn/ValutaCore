
namespace ValutaCore.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class LoginController(ISender mediator) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] SignInRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        try
        {
            var response = await mediator.Send(
                new SignInCommand(request.Username, request.Password, ip));

            return Ok(response);
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}