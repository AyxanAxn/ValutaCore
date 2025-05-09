using ValutaCore.Api.Models;

namespace ValutaCore.Api.Middleware
{
    public class GlobalErrorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalErrorMiddleware(
            RequestDelegate next,
            ILogger<GlobalErrorMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "Unhandled exception occurred processing request {Method} {Path}: {ErrorMessage}", 
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);
                
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, userMessage) = DetermineStatusCodeAndMessage(exception);
            context.Response.StatusCode = (int)statusCode;

            var errorResponse = CreateErrorResponse(context, exception, statusCode, userMessage);
            await WriteJsonResponseAsync(context, errorResponse);
        }

        private (HttpStatusCode StatusCode, string Message) DetermineStatusCodeAndMessage(Exception exception)
        {
            return exception switch
            {
                // Resource not found errors
                KeyNotFoundException => (
                    HttpStatusCode.NotFound, 
                    "The requested resource was not found"),
                
                // Authentication/Authorization errors
                UnauthorizedAccessException => (
                    HttpStatusCode.Unauthorized, 
                    "You are not authorized to access this resource"),
                
                // Input validation errors
                ArgumentException or 
                FormatException or 
                InputValidationException => (
                    HttpStatusCode.BadRequest, 
                    exception.Message),
                
                // Default case - server errors
                _ => (
                    HttpStatusCode.InternalServerError,
                    _env.IsDevelopment() 
                        ? exception.Message 
                        : "An unexpected server error occurred. Please try again later")
            };
        }

        private ErrorResponse CreateErrorResponse(
            HttpContext context, 
            Exception exception, 
            HttpStatusCode statusCode, 
            string message)
        {
            return new ErrorResponse
            {
                Status = statusCode,
                Message = message,
                Details = _env.IsDevelopment() ? exception.ToString() : null,
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            };
        }

        private static async Task WriteJsonResponseAsync(HttpContext context, ErrorResponse error)
        {
            var options = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            var serializedError = JsonSerializer.Serialize(error, options);
            await context.Response.WriteAsync(serializedError);
        }
    }
    
    public class InputValidationException : Exception
    {
        public InputValidationException(string message) : base(message) { }
        
        public InputValidationException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}