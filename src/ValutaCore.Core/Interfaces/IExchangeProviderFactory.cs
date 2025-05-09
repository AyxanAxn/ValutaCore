namespace ValutaCore.Core.Interfaces;

public interface IExchangeProviderFactory
{
    IExchangeProvider GetProvider(string providerName = null!);
    IEnumerable<string> GetAvailableProviders();
}