using FluentValidation;

namespace ValutaCore.Application.Currency.Queries.GetLatestRates;

public class GetLatestRatesValidator : AbstractValidator<GetLatestRatesQuery>
{
    public GetLatestRatesValidator()
    {
        RuleFor(x => x.BaseCurrency).NotEmpty().Length(3);
    }
}