using FluentValidation;

namespace ValutaCore.Application.Currency.Queries.ConvertCurrency;

public class ConvertCurrencyValidator : AbstractValidator<ConvertCurrencyQuery>
{
    public ConvertCurrencyValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.SourceCurrency).NotEmpty().Length(3);
        RuleFor(x => x.TargetCurrency).NotEmpty().Length(3);
    }
}