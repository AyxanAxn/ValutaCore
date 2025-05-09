using ValutaCore.Api.Models;

namespace ValutaCore.Tests.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<GlobalErrorMiddleware>> _mockLogger = new();
        private readonly Mock<IHostEnvironment> _mockEnvironment = new();

        [Fact]
        public async Task InvokeAsync_WithNoException_CallsNext()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var nextCalled = false;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            // Create a new middleware instance with our test delegate
            var middleware = new GlobalErrorMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_WithArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new ArgumentException("Invalid argument");
            };

            // Create a new middleware instance with our test delegate
            var middleware = new GlobalErrorMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal("Invalid argument", error.Message);
            Assert.Equal(HttpStatusCode.BadRequest, error.Status);
        }

        [Fact]
        public async Task InvokeAsync_WithValidationException_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new InputValidationException("Validation failed");
            };

            // Create a new middleware instance with our test delegate
            var middleware = new GlobalErrorMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal("Validation failed", error?.Message);
            Assert.Equal(HttpStatusCode.BadRequest, error?.Status);
        }
        
        [Fact]
        public async Task InvokeAsync_WithFormatException_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new FormatException("Invalid format");
            };

            // Create a new middleware instance with our test delegate
            var middleware = new GlobalErrorMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal("Invalid format", error.Message);
            Assert.Equal(HttpStatusCode.BadRequest, error.Status);
        }
        
        [Fact]
        public async Task InvokeAsync_WithException_SetsTraceId()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.TraceIdentifier = "test-trace-id";

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new Exception("Something went wrong");
            };

            var middleware = new GlobalErrorMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.Invoke(context);

            // Assert
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal("test-trace-id", error.TraceId);
        }
        
        [Fact]
        public async Task InvokeAsync_WithException_SetsTimestamp()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new Exception("Something went wrong");
            };

            var middleware = new GlobalErrorMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.Invoke(context);

            // Assert
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Check that the timestamp is recent (within the last minute)
            var now = DateTime.UtcNow;
            Assert.NotNull(error!.Timestamp);
            var timestampDiff = now - error.Timestamp;
            Assert.True(timestampDiff.TotalMinutes < 1);
        }
    }
}