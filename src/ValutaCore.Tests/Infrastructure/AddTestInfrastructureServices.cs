namespace ValutaCore.Tests.Infrastructure
{
    public static class TestInfrastructureExtensions
    {
        public static IServiceCollection AddTestInfrastructureServices(
            this IServiceCollection services,
            IConfiguration config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));

            // JWT settings
            services.Configure<JwtSettings>(config.GetSection("JwtSettings"));

            // Auth & provider factories
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddSingleton<IExchangeProviderFactory, ExchangeProviderFactory>();

            // Primary exchange-rate provider (without HttpClient factory)
            services.AddTransient<FrankfurterApiProvider>();
            services.AddTransient<IExchangeProvider, FrankfurterApiProvider>();

            return services;
        }
    }
}