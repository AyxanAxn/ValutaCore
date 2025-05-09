namespace ValutaCore.Core.Models.Currency;

public class ExchangeRequest
{
    public decimal Amount { get; init; }
    public string SourceCurrency { get; init; } = string.Empty;
    public string TargetCurrency { get; init; } = string.Empty;
}