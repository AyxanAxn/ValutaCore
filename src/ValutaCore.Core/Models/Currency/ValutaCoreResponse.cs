namespace ValutaCore.Core.Models.Currency;

public class ValutaCoreResponse
{
    public decimal Amount { get; init; }
    public string FromCurrency { get; init; } = string.Empty;
    public string ToCurrency { get; init; } = string.Empty;
    public decimal ConvertedAmount { get; init; }
    public DateTime Date { get; init; }
    public decimal Rate { get; init; }
}