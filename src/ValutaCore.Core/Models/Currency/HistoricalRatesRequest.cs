namespace ValutaCore.Core.Models.Currency;

public class HistoricalRatesRequest
{
    public string BaseCurrency { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}