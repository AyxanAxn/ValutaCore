namespace ValutaCore.Application.Currency.Queries.GetLatestRates;

public record GetLatestRatesQuery(string BaseCurrency)
    : IRequest<IEnumerable<RateDto>>;