namespace ValutaCore.Api.Models;

public class ErrorResponse
{
    /// HTTP status code of the response
    public HttpStatusCode Status { get; init; }
        
    /// User-friendly error message
    public string Message { get; init; } = string.Empty;
        
    /// Technical details of the error (only in development)
    public string? Details { get; init; }
        
    /// Unique identifier for the request to correlate with logs
    public string? TraceId { get; init; }
        
    /// When the error occurred (UTC)
    public DateTime Timestamp { get; init; }
}