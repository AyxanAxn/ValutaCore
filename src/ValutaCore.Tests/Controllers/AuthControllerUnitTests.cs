namespace ValutaCore.Tests.Controllers;

public class AuthControllerUnitTests
{
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly AuthenticationController          _controller;

    public AuthControllerUnitTests()
    {
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        var loggerMock       = new Mock<ILogger<AuthenticationController>>();
        var settingsMock     = new Mock<IOptions<CredentialSettings>>();

        settingsMock.Setup(s => s.Value).Returns(new CredentialSettings
        {
            Users =
            [
                new CredentialProfile { Username = "admin", Password = "admin", Roles = ["Admin"] },
                new CredentialProfile { Username = "user",  Password = "user",  Roles = ["User"]  }
            ]
        });

        _controller = new AuthenticationController(
            _jwtTokenServiceMock.Object,
            loggerMock.Object,
            settingsMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Connection = { RemoteIpAddress = IPAddress.Loopback }
                }
            }
        };
    }

    [Fact]
    public void Login_WithAdminCredentials_ReturnsOkResultWithToken()
    {
        // Arrange
        const string jwt = "dummy-jwt";
        _jwtTokenServiceMock
            .Setup(s => s.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(jwt);

        var request = new SignInRequest { Username = "admin", Password = "admin" };

        // Act
        var actionResult = _controller.Login(request);

        // Assert
        var ok   = Assert.IsType<OkObjectResult>(actionResult);
        var resp = Assert.IsType<SignInResponse>(ok.Value);
        Assert.Equal("admin", resp.Username);
        Assert.Equal(jwt, resp.Token);
        Assert.Contains("Admin", resp.Roles);
    }

    [Fact]
    public void Login_WithStandardUserCredentials_ReturnsOkResultWithToken()
    {
        const string jwt = "dummy-jwt";
        _jwtTokenServiceMock
            .Setup(s => s.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(jwt);

        var actionResult = _controller.Login(new SignInRequest { Username = "user", Password = "user" });

        var ok   = Assert.IsType<OkObjectResult>(actionResult);
        var resp = Assert.IsType<SignInResponse>(ok.Value);
        Assert.Equal("user", resp.Username);
        Assert.Equal(jwt,   resp.Token);
        Assert.Contains("User", resp.Roles);
    }

    [Fact]
    public void Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var result = _controller.Login(new SignInRequest { Username = "admin", Password = "wrong" });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Login_WithUnknownUser_ReturnsUnauthorized()
    {
        var result = _controller.Login(new SignInRequest { Username = "ghost", Password = "pw" });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Login_WithEmptyUsername_ReturnsBadRequest()
    {
        var result = _controller.Login(new SignInRequest { Username = "", Password = "pw" });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Login_WithEmptyPassword_ReturnsBadRequest()
    {
        var result = _controller.Login(new SignInRequest { Username = "admin", Password = "" });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Login_WithNullCredentials_ReturnsBadRequest()
    {
        var result = _controller.Login(new SignInRequest { Username = null!, Password = null! });
        Assert.IsType<BadRequestObjectResult>(result);
    }
}