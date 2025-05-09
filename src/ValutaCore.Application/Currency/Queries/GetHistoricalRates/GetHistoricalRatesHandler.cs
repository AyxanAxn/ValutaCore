namespace ValutaCore.Application.Currency.Queries.GetHistoricalRates;

public class GetHistoricalRatesHandler(IValutaService svc)
    : IRequestHandler<GetHistoricalRatesQuery, IEnumerable<HistoricalRateDto>>
{
    public async Task<IEnumerable<HistoricalRateDto>> Handle(
        GetHistoricalRatesQuery q, CancellationToken ct)
    {
        if (ct.IsCancellationRequested is true)
            return new List<HistoricalRateDto>();
        
        var req = new HistoricalRatesRequest
        {
            BaseCurrency = q.BaseCurrency,
            StartDate    = q.StartDate,
            EndDate      = q.EndDate,
            Page         = q.Page,
            PageSize     = q.PageSize
        };

        var raw = await svc.GetHistoricalRatesAsync(req);

        return raw.Items.SelectMany(resp =>
            resp.ExchangeRates.Select(kv => new HistoricalRateDto(resp.Date, kv.Key, kv.Value)));
    }

}