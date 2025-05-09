namespace ValutaCore.Core.Models.Currency;

public class RateHistoryEntry
{
    public DateTime Date { get; init; }
    public string BaseCurrencyCode { get; init; } = string.Empty;
    public Dictionary<string, decimal> ExchangeRates { get; init; } = new();
}