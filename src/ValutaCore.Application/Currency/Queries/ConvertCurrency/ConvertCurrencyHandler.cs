namespace ValutaCore.Application.Currency.Queries.ConvertCurrency;

public class ConvertCurrencyHandler(IValutaService svc) : IRequestHandler<ConvertCurrencyQuery, ConversionDto>
{
    public async Task<ConversionDto> Handle(ConvertCurrencyQuery q, CancellationToken ct)
    {
        if (ct.IsCancellationRequested is true)
            return new ConversionDto(0, string.Empty, string.Empty, 0);
        
        var req = new ExchangeRequest
        {
            Amount = q.Amount,
            SourceCurrency = q.SourceCurrency,
            TargetCurrency = q.TargetCurrency
        };

        var result = await svc.ConvertCurrencyAsync(req);

        return new ConversionDto(q.Amount, q.SourceCurrency, q.TargetCurrency, result.ConvertedAmount);
    }
}