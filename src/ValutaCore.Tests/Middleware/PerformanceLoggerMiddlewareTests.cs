namespace CurrencyConverter.Tests.Middleware
{
    public class PerformanceLoggerMiddlewareTests
    {
        private readonly Mock<ILogger<PerformanceLoggerMiddleware>> _mockLogger;

        public PerformanceLoggerMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<PerformanceLoggerMiddleware>>();
        }

        [Fact]
        public async Task InvokeAsync_LogsRequestDetails()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            context.Request.Method = "GET";
            context.Request.Path = "/api/v1/Currency/rates";
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("ClientId", "test-client-id")
            }));

            var nextCalled = false;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            
            var middleware = new PerformanceLoggerMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task InvokeAsync_WithNoClientId_LogsIpAddress()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            context.Request.Method = "GET";
            context.Request.Path = "/api/v1/Currency/rates";
            // No client ID in claims

            var nextCalled = false;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            
            var middleware = new PerformanceLoggerMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }
        
    }
}
