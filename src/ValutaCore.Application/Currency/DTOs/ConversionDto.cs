namespace ValutaCore.Application.Currency.DTOs;

public record ConversionDto(decimal OriginalAmount,
    string SourceCurrency,
    string TargetCurrency,
    decimal ConvertedAmount);