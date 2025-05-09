using ValutaCore.Core.Mapping;

namespace ValutaCore.Core;

public static class RegisterCore
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IValutaService, ValutaService>();
        services.AddMapster();
        return services;
    }
}