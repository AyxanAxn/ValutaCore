namespace ValutaCore.Core.Models.Currency;

public class ExchangeRateResponse
{
    [JsonPropertyName("amount")] public decimal Amount { get; init; }

    [JsonPropertyName("base")] public string BaseCurrency { get; init; } = string.Empty;

    [JsonPropertyName("date")] public DateTime Date { get; init; }

    [JsonPropertyName("rates")] public Dictionary<string, decimal> Rates { get; init; } = new();
}