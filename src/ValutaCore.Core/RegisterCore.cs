using ValutaCore.Core.Mapping;

namespace ValutaCore.Core;

public static class RegisterCore
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrencyService, ValutaService>();
        services.AddMapster();
        return services;
    }
}