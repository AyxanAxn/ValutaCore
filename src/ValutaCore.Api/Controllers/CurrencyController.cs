namespace ValutaCore.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CurrencyController(ISender mediator) : ControllerBase
{
    // GET /api/v1/currency/rates?baseCurrency=USD
    [HttpGet("rates")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetLatestRates(
        [Required][FromQuery] string baseCurrency)
    {
        var result = await mediator.Send(
            new GetLatestRatesQuery(baseCurrency));

        return Ok(result);              // IEnumerable<RateDto>
    }

    // GET /api/v1/currency/convert?value=100&sourceCurrency=USD&targetCurrency=EUR
    [HttpGet("convert")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> ConvertCurrency(
        [Required][FromQuery] decimal value,
        [Required][FromQuery] string sourceCurrency,
        [Required][FromQuery] string targetCurrency)
    {
        var result = await mediator.Send(
            new ConvertCurrencyQuery(value, sourceCurrency, targetCurrency));

        return Ok(result);              // ConversionDto
    }

    // GET /api/v1/currency/historical?baseCurrency=USD&startDate=2024-01-01...
    [HttpGet("historical")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetHistoricalRates(
        [Required][FromQuery] string   baseCurrency,
        [Required][FromQuery] DateTime startDate,
        [Required][FromQuery] DateTime endDate,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await mediator.Send(
            new GetHistoricalRatesQuery(baseCurrency, startDate, endDate, page, pageSize));

        return Ok(result);              // IEnumerable<HistoricalRateDto>
    }
}
