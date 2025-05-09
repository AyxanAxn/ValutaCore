namespace ValutaCore.Application.Currency.DTOs;

public record HistoricalRateDto(
    DateTime Date,
    string Currency,
    decimal Rate);