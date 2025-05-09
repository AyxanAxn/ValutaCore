namespace ValutaCore.Api.Models;

internal class RequestInfo
{
    public string ClientIp { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string QueryString { get; init; } = string.Empty;
}