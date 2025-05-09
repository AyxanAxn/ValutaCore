namespace ValutaCore.Api.Middleware
{
    public class PerformanceLoggerMiddleware(
        RequestDelegate next,
        ILogger<PerformanceLoggerMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var clientId = context.User.FindFirst("ClientId")?.Value ?? "Unknown";
            var method = context.Request.Method;
            var path = context.Request.Path;
            var queryString = context.Request.QueryString.ToString();

            var originalBodyStream = context.Response.Body;

            try
            {
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await next(context);

                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                context.Response.Headers.Add("X-Response-Time-Ms", elapsedMs.ToString());

                logger.LogInformation(
                    "Request: {Path}{QueryString} | Client: {ClientIp} | ClientId: {ClientId} | Method: {Method} | Response: {StatusCode} | Duration: {ElapsedMs}ms",
                    path,
                    queryString,
                    clientIp,
                    clientId,
                    method,
                    statusCode,
                    elapsedMs);

                responseBodyStream.Seek(0, SeekOrigin.Begin);

                await responseBodyStream.CopyToAsync(originalBodyStream);
                
                context.Response.Body = originalBodyStream;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                
                logger.LogError(ex,
                    "Request failed: {Path}{QueryString} | Client: {ClientIp} | ClientId: {ClientId} | Method: {Method} | Duration: {ElapsedMs}ms",
                    path,
                    queryString,
                    clientIp,
                    clientId,
                    method,
                    elapsedMs);
                
                context.Response.Body = originalBodyStream;
                
                throw;
            }
        }
    }
}
