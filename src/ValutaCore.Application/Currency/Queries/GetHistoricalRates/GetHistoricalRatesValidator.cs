using FluentValidation;

namespace ValutaCore.Application.Currency.Queries.GetHistoricalRates;

public class GetHistoricalRatesValidator : AbstractValidator<GetHistoricalRatesQuery>
{
    public GetHistoricalRatesValidator()
    {
        RuleFor(x => x.BaseCurrency).NotEmpty().Length(3);
        RuleFor(x => x.StartDate).LessThanOrEqualTo(x => x.EndDate);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}