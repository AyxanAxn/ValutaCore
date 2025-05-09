namespace ValutaCore.Application.Currency.Queries.ConvertCurrency;

public record ConvertCurrencyQuery(decimal Amount,
    string SourceCurrency,
    string TargetCurrency)
    : IRequest<ConversionDto>;