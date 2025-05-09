namespace ValutaCore.Api.Middleware
{
    public class GlobalErrorMiddleware(
        RequestDelegate next,
        ILogger<GlobalErrorMiddleware> logger,
        IHostEnvironment env)
    {
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception during request processing");
                await HandleAsync(context, ex);
            }
        }

        private async Task HandleAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            // Determine status code and user‐facing message
            var (status, message) = ex switch
            {
                KeyNotFoundException                  => (HttpStatusCode.NotFound, "Requested resource was not found."),
                UnauthorizedAccessException            => (HttpStatusCode.Unauthorized, "Access is denied."),
                ArgumentException or FormatException or InputValidationException 
                                                      => (HttpStatusCode.BadRequest, ex.Message),
                _                                      => (HttpStatusCode.InternalServerError,
                                                             env.IsDevelopment()
                                                                 ? ex.Message
                                                                 : "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = (int)status;

            var error = new ErrorDetail
            {
                Status    = status,
                Message   = message,
                Details   = env.IsDevelopment() ? ex.ToString() : null,
                TraceId   = Activity.Current?.Id ?? context.TraceIdentifier
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var payload = JsonSerializer.Serialize(error, options);
            await context.Response.WriteAsync(payload);
        }
    }

    public class ErrorDetail
    {
        public HttpStatusCode Status      { get; init; }
        public string         Message     { get; init; } = string.Empty;
        public string?        Details     { get; init; }
        public string?        TraceId     { get; init; }
    }

    public class InputValidationException : Exception
    {
        public InputValidationException(string message) : base(message) { }
    }
}
