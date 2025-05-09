namespace ValutaCore.Tests.Middleware
{
    public class RateLimitingMiddlewareTests
    {
        private readonly Mock<ILogger<RateLimitingMiddleware>> _mockLogger;
        private readonly IMemoryCache _memoryCache;
        private const int Limit = 100;
        private const int PeriodInMinutes = 1;

        public RateLimitingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            RequestDelegate next = (HttpContext context) => Task.CompletedTask;
            var rateLimitingMiddleware = new RateLimitingMiddleware(next, _memoryCache, _mockLogger.Object, Limit, PeriodInMinutes);
        }

        [Fact]
        public async Task InvokeAsync_FirstRequest_AllowsRequest()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("ClientId", "test-client-id")
                })),
                Request =
                {
                    Method = "GET",
                    Path = "/api/v1/Currency/rates"
                }
            };
            var nextCalled = false;

            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            
            // Create a new middleware instance with our test delegate
            var middleware = new RateLimitingMiddleware(next, _memoryCache, _mockLogger.Object, Limit, PeriodInMinutes);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal((int)HttpStatusCode.OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ExceedsRateLimit_ReturnsTooManyRequests()
        {
            // Arrange
            const string clientId = "test-client-id";
            const string path = "/api/v1/Currency/rates";
            const string cacheKey = $"RateLimit_{clientId}_{path}";

            // Set up the cache to simulate rate limit exceeded
            _memoryCache.Set(cacheKey, Limit, TimeSpan.FromMinutes(1));

            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("ClientId", clientId)
                })),
                Request =
                {
                    Method = "GET",
                    Path = path
                },
                Response =
                {
                    Body = new MemoryStream()
                }
            };

            var nextCalled = false;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            
            // Create a new middleware instance with our test delegate
            var middleware = new RateLimitingMiddleware(next, _memoryCache, _mockLogger.Object, Limit, PeriodInMinutes);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.False(nextCalled);
            Assert.Equal((int)HttpStatusCode.TooManyRequests, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithNoClientId_UsesIpAddress()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            context.Request.Method = "GET";
            context.Request.Path = "/api/v1/Currency/rates";
            var nextCalled = false;

            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            
            // Create a new middleware instance with our test delegate
            var middleware = new RateLimitingMiddleware(next, _memoryCache, _mockLogger.Object, Limit, PeriodInMinutes);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal((int)HttpStatusCode.OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_MultipleRequestsWithinLimit_AllowsRequests()
        {
            // Arrange
            var clientId = "test-client-id";
            var path = "/api/v1/Currency/rates";

            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("ClientId", clientId)
            }));
            context.Request.Method = "GET";
            context.Request.Path = path;

            var nextCalled = 0;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled++;
                return Task.CompletedTask;
            };
            
            // Create a new middleware instance with our counting delegate
            var middleware = new RateLimitingMiddleware(next, _memoryCache, _mockLogger.Object, Limit, PeriodInMinutes);

            // Act - Make 5 requests (below the limit of 100)
            for (int i = 0; i < 5; i++)
            {
                await middleware.InvokeAsync(context);
            }

            // Assert
            Assert.Equal(5, nextCalled);
            Assert.Equal((int)HttpStatusCode.OK, context.Response.StatusCode);
        }
    }
}
