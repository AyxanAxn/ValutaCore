namespace ValutaCore.Application.Currency.Queries.GetLatestRates;

public class GetLatestRatesHandler(IValutaService svc)
    : IRequestHandler<GetLatestRatesQuery, IEnumerable<RateDto>>
{
    public async Task<IEnumerable<RateDto>> Handle(GetLatestRatesQuery q,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested is true)
            return new List<RateDto>();
        
        var raw = await svc.GetLatestRatesAsync(q.BaseCurrency);  

        return raw.Rates
            .Select(kv => new RateDto(kv.Key, kv.Value));    
    }
}