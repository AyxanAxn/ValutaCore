namespace ValutaCore.Core.Interfaces;

public interface IValutaService
{
    Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency);
    Task<ValutaCoreResponse> ConvertCurrencyAsync(ExchangeRequest request);
    Task<PaginatedResponse<RateHistoryEntry>> GetHistoricalRatesAsync(HistoricalRatesRequest request);
}