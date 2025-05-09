namespace ValutaCore.Api.Middleware
{
    public class RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger,
        int requestLimit = 100,
        int timeWindowMinutes = 10)
    {
        private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(timeWindowMinutes);

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = context.User.FindFirst("ClientId")?.Value;
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var endpoint = context.Request.Path;

            var requestKey = !string.IsNullOrEmpty(clientId)
                ? $"RateLimit_{clientId}_{endpoint}"
                : $"RateLimit_{clientIp}_{endpoint}";

            if (!cache.TryGetValue(requestKey, out int requestCount))
            {
                requestCount = 0;
            }

            if (requestCount >= requestLimit)
            {
                logger.LogWarning("Rate limit exceeded for client {ClientIdentifier} on endpoint {Endpoint}",
                    !string.IsNullOrEmpty(clientId) ? clientId : clientIp, endpoint);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";

                var response = JsonSerializer.Serialize(new
                {
                    Status = 429,
                    Message = "Too many requests. Please try again later."
                });

                await context.Response.WriteAsync(response);
                return;
            }

            cache.Set(requestKey, requestCount + 1, _timeWindow);

            await next(context);
        }
    }
}
