using ValutaCore.Api.Models;

namespace ValutaCore.Api.Middleware
{
    /// <summary>
    /// Middleware for logging request performance metrics and details
    /// </summary>
    public class PerformanceLoggerMiddleware(
        RequestDelegate next,
        ILogger<PerformanceLoggerMiddleware> logger)
    {
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
        private readonly ILogger<PerformanceLoggerMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;
            
            // Extract request details early
            var requestInfo = ExtractRequestInfo(context);

            try
            {
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                // Process the request through the pipeline
                await _next(context);

                stopwatch.Stop();
                
                // Add timing information to response headers
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                context.Response.Headers["X-Response-Time-Ms"] = elapsedMs.ToString();

                // Log successful request
                LogSuccessfulRequest(requestInfo, context.Response.StatusCode, elapsedMs);

                // Copy the response to the original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Log failed request
                LogFailedRequest(requestInfo, ex, stopwatch.ElapsedMilliseconds);
                
                // Restore the original response body
                context.Response.Body = originalBodyStream;
                
                throw; // Re-throw the exception to be handled by other middleware
            }
            finally
            {
                // Ensure response body is restored
                context.Response.Body = originalBodyStream;
            }
        }

        private RequestInfo ExtractRequestInfo(HttpContext context)
        {
            return new RequestInfo
            {
                ClientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                ClientId = context.User.FindFirst("ClientId")?.Value ?? "Anonymous",
                Method = context.Request.Method,
                Path = context.Request.Path.ToString(),
                QueryString = context.Request.QueryString.ToString()
            };
        }

        private void LogSuccessfulRequest(RequestInfo requestInfo, int statusCode, long elapsedMs)
        {
            _logger.LogInformation(
                "[API Request] {Method} {PathWithQuery} | Status: {StatusCode} | Client: {ClientIp} | ID: {ClientId} | Duration: {ElapsedMs}ms",
                requestInfo.Method,
                CombinePathAndQuery(requestInfo.Path, requestInfo.QueryString),
                statusCode,
                requestInfo.ClientIp,
                requestInfo.ClientId,
                elapsedMs);
        }

        private void LogFailedRequest(RequestInfo requestInfo, Exception exception, long elapsedMs)
        {
            _logger.LogError(
                exception,
                "[API Failure] {Method} {PathWithQuery} | Client: {ClientIp} | ID: {ClientId} | Duration: {ElapsedMs}ms | Error: {ErrorMessage}",
                requestInfo.Method,
                CombinePathAndQuery(requestInfo.Path, requestInfo.QueryString),
                requestInfo.ClientIp,
                requestInfo.ClientId,
                elapsedMs,
                exception.Message);
        }

        private string CombinePathAndQuery(string path, string queryString)
        {
            return string.IsNullOrEmpty(queryString) ? path : $"{path}{queryString}";
        }
        }
}