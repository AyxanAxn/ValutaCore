namespace ValutaCore.Api;

public static class RegisterApi
{
    public static IServiceCollection AddApiServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers()

            // 🔑 add this — point MVC at the assembly that contains the controllers
            .AddApplicationPart(typeof(RegisterApi).Assembly)

            // existing JSON options
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // …everything else in your method stays the same …
        services.Configure<CredentialSettings>(configuration.GetSection(CredentialSettings.SectionName));
        ConfigureAuthentication.ConfigureAuth(services, configuration);
        ConfigureSwagger.ConfigureSwaggerAPI(services);

        services.AddApiVersioning(o =>
        {
            o.ReportApiVersions = true;
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
            o.ApiVersionReader = new Microsoft.AspNetCore.Mvc.Versioning.UrlSegmentApiVersionReader();
        });

        services.AddVersionedApiExplorer(o =>
        {
            o.GroupNameFormat = "'v'VVV";
            o.SubstituteApiVersionInUrl = true;
        });

        services.AddMemoryCache();

        services.AddOpenTelemetry()
            .WithTracing(t => t.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        return services;
    }
}