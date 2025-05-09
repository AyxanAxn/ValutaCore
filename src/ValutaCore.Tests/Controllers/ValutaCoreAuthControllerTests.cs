namespace ValutaCore.Tests.Controllers;

public class ValutaCoreAuthControllerTests
{
    private readonly Mock<IJwtTokenService>             _jwtTokenServiceMock;
    private readonly AuthenticationController                      _sut; // system-under-test

    public ValutaCoreAuthControllerTests()
    {
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        Mock<ILogger<AuthenticationController>> loggerMock = new();
        if (loggerMock == null) throw new ArgumentNullException(nameof(loggerMock));
        Mock<IOptions<CredentialSettings>> settingsMock = new();

        settingsMock.Setup(x => x.Value).Returns(new CredentialSettings
        {
            Users =
            [
                new CredentialProfile { Username = "admin", Password = "admin", Roles = ["Admin"] },
                new CredentialProfile { Username = "user",  Password = "user",  Roles = ["User"]  }
            ]
        });

        _sut = new AuthenticationController(
            _jwtTokenServiceMock.Object,
            loggerMock.Object,
            settingsMock.Object);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Connection = { RemoteIpAddress = IPAddress.Loopback }
            }
        };
    }

    [Fact]
    public void Login_AdminCredentials_ShouldReturnOkWithToken()
    {
        // Arrange
        const string token = "dummy-jwt";
        _jwtTokenServiceMock
            .Setup(s => s.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(token);

        var request = new SignInRequest { Username = "admin", Password = "admin" };

        // Act
        var actionResult = _sut.Login(request);

        // Assert
        var ok          = Assert.IsType<OkObjectResult>(actionResult);
        var responseDto = Assert.IsType<SignInResponse>(ok.Value);
        Assert.Equal("admin", responseDto.Username);
        Assert.Equal(token,   responseDto.Token);
        Assert.Contains("Admin", responseDto.Roles);
    }

    [Fact]
    public void Login_UserCredentials_ShouldReturnOkWithToken()
    {
        const string token = "dummy-jwt";
        _jwtTokenServiceMock
            .Setup(s => s.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(token);

        var result = _sut.Login(new SignInRequest { Username = "user", Password = "user" });

        var ok   = Assert.IsType<OkObjectResult>(result);
        var resp = Assert.IsType<SignInResponse>(ok.Value);
        Assert.Equal("user", resp.Username);
        Assert.Equal(token,  resp.Token);
        Assert.Contains("User", resp.Roles);
    }

    [Fact]
    public void Login_WrongPassword_ShouldReturnUnauthorized()
    {
        var result = _sut.Login(new SignInRequest { Username = "admin", Password = "bad" });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Login_UnknownUser_ShouldReturnUnauthorized()
    {
        var result = _sut.Login(new SignInRequest { Username = "ghost", Password = "pw" });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Login_EmptyUsername_ShouldReturnBadRequest()
    {
        var result = _sut.Login(new SignInRequest { Username = "", Password = "pw" });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Login_EmptyPassword_ShouldReturnBadRequest()
    {
        var result = _sut.Login(new SignInRequest { Username = "admin", Password = "" });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Login_NullCredentials_ShouldReturnBadRequest()
    {
        var result = _sut.Login(new SignInRequest { Username = null!, Password = null! });
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
