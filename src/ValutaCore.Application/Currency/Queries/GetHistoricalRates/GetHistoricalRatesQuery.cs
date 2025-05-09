namespace ValutaCore.Application.Currency.Queries.GetHistoricalRates;

public record GetHistoricalRatesQuery(string BaseCurrency,
    DateTime StartDate,
    DateTime EndDate,
    int Page,
    int PageSize)
    : IRequest<IEnumerable<HistoricalRateDto>>;