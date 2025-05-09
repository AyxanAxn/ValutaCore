namespace ValutaCore.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CurrencyController(ICurrencyService currencyService) : ControllerBase
{
    private readonly ICurrencyService _currencyService =
        currencyService ?? throw new ArgumentNullException(nameof(currencyService));

    [HttpGet("rates")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetLatestRates([Required][FromQuery] string baseCurrency)
    {
        if (string.IsNullOrEmpty(baseCurrency))
            return BadRequest("Base currency is required");

        var rates = await _currencyService.GetLatestRatesAsync(baseCurrency);
        return Ok(rates);
    }

    [HttpGet("convert")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> ConvertCurrency(
        [Required][FromQuery] decimal value,
        [Required][FromQuery] string sourceCurrency,
        [Required][FromQuery] string targetCurrency)
    {
        if (value <= 0)
            return BadRequest("Amount must be greater than zero");

        if (string.IsNullOrEmpty(sourceCurrency) || string.IsNullOrEmpty(targetCurrency))
            return BadRequest("Source and target currencies must be specified");

        var request = new ExchangeRequest
        {
            Amount = value,
            SourceCurrency = sourceCurrency,
            TargetCurrency = targetCurrency
        };

        var conversion = await _currencyService.ConvertCurrencyAsync(request);
        return Ok(conversion);
    }

    [HttpGet("historical")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetHistoricalRates(
        [Required][FromQuery] string baseCurrency,
        [Required][FromQuery] DateTime startDate,
        [Required][FromQuery] DateTime endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrEmpty(baseCurrency))
            return BadRequest("Base currency is required");

        if (startDate > endDate)
            return BadRequest("Start date must be before or equal to end date");

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var request = new HistoricalRatesRequest
        {
            BaseCurrency = baseCurrency,
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize
        };

        var history = await _currencyService.GetHistoricalRatesAsync(request);
        return Ok(history);
    }
}
