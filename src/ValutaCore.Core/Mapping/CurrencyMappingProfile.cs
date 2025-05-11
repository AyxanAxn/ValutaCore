using Mapster;

namespace ValutaCore.Core.Mapping;

public class CurrencyMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<(ExchangeRateResponse Source, decimal Amount, string FromCurrency, string ToCurrency),
                ValutaCoreResponse>()
            .Map(dest => dest.Amount, src => src.Amount)
            .Map(dest => dest.FromCurrency, src => src.FromCurrency)
            .Map(dest => dest.ToCurrency, src => src.ToCurrency)
            .Map(dest => dest.ConvertedAmount, src => src.Amount * src.Source.Rates[src.ToCurrency])
            .Map(dest => dest.Rate, src => src.Source.Rates[src.ToCurrency])
            .Map(dest => dest.Date, src => src.Source.Date);

        config.NewConfig<ExchangeRateResponse, ValutaCoreResponse>()
            .Map(dest => dest.Amount, src => src.Amount)
            .Map(dest => dest.FromCurrency, src => src.BaseCurrency)
            .Map(dest => dest.ToCurrency, src => src.Rates.Keys.FirstOrDefault())
            .Map(dest => dest.ConvertedAmount,
                src => src.Amount * (src.Rates.Any() ? src.Rates.Values.FirstOrDefault() : 1))
            .Map(dest => dest.Rate, src => src.Rates.Any() ? src.Rates.Values.FirstOrDefault() : 1)
            .Map(dest => dest.Date, src => src.Date);

        config.NewConfig<ExchangeRequest, ExchangeRateResponse>()
            .Map(dest => dest.Amount, src => src.Amount)
            .Map(dest => dest.BaseCurrency, src => src.SourceCurrency)
            .Map(dest => dest.Date, _ => DateTime.UtcNow)
            .Map(dest => dest.Rates, src => new Dictionary<string, decimal> { { src.TargetCurrency, 1.0m } });

        config
            .NewConfig<(Dictionary<DateTime, Dictionary<string, decimal>> Data, string BaseCurrency),
                List<RateHistoryEntry>>()
            .MapWith(src => src.Data.Select(kvp => new RateHistoryEntry
            {
                Date = kvp.Key,
                BaseCurrencyCode = src.BaseCurrency,
                ExchangeRates = kvp.Value
            }).ToList());

        config
            .NewConfig<(HistoricalRatesRequest Request, List<RateHistoryEntry> AllRates),
                PaginatedResponse<RateHistoryEntry>>()
            .MapWith(src => new PaginatedResponse<RateHistoryEntry>
            {
                Items = src.AllRates
                    .Skip((src.Request.Page - 1) * src.Request.PageSize)
                    .Take(src.Request.PageSize)
                    .ToList(),
                Page = src.Request.Page,
                PageSize = src.Request.PageSize,
                TotalCount = src.AllRates.Count
            });

        config
            .NewConfig<(ExchangeRequest Request, decimal Rate, decimal ConvertedAmount, DateTime Date),
                ValutaCoreResponse>()
            .Map(dest => dest.Amount, src => src.Request.Amount)
            .Map(dest => dest.FromCurrency, src => src.Request.SourceCurrency)
            .Map(dest => dest.ToCurrency, src => src.Request.TargetCurrency)
            .Map(dest => dest.ConvertedAmount, src => src.ConvertedAmount)
            .Map(dest => dest.Rate, src => src.Rate)
            .Map(dest => dest.Date, src => src.Date);
    }
}