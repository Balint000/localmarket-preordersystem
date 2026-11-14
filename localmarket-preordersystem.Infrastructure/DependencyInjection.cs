using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Infrastructure.Persistence.Context;
using localmarket_preordersystem.Infrastructure.Persistence.Repositories;
using localmarket_preordersystem.Infrastructure.Auth;
using localmarket_preordersystem.Infrastructure.ExternalAPIs.Geocoding;
using localmarket_preordersystem.Infrastructure.ExternalAPIs.Weather;
using localmarket_preordersystem.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace localmarket_preordersystem.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"A '{ConnectionStringName}' connection string hiányzik. " +
                "Állítsd be User Secretsszel vagy a ConnectionStrings__DefaultConnection környezeti változóval.");
        }

        services.AddDbContext<MarketDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMarketRepository, MarketRepository>();
        services.AddScoped<IProducerRepository, ProducerRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPickupSlotRepository, PickupSlotRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IReportQueries, ReportQueries>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<IGeocodingService, OpenMeteoGeocodingService>(client =>
        {
            client.BaseAddress = new Uri("https://geocoding-api.open-meteo.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.Configure<ImageStorageOptions>(configuration.GetSection(ImageStorageOptions.SectionName));
        services.AddSingleton<IImageStorage, LocalImageStorage>();

        return services;
    }
}
