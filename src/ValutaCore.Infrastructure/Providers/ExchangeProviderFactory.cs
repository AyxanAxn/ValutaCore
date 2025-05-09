namespace ValutaCore.Infrastructure.Providers;

public class ExchangeProviderFactory(IServiceProvider serviceProvider) : IExchangeProviderFactory
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly Dictionary<string, Type> _providerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Frankfurter", typeof(FrankfurterApiProvider) }
    };

    public IExchangeProvider GetProvider(string providerName = null!)
    {
        if (string.IsNullOrEmpty(providerName))
        {
            providerName = "Frankfurter";
        }

        if (!_providerTypes.TryGetValue(providerName, out var providerType))
        {
            throw new ArgumentException($"Provider '{providerName}' is not supported.");
        }

        return (IExchangeProvider)_serviceProvider.GetRequiredService(providerType);
    }

    public IEnumerable<string> GetAvailableProviders()
    {
        return _providerTypes.Keys;
    }
}