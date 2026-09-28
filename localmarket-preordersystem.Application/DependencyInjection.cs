using localmarket_preordersystem.Application.Auth.Login;
using localmarket_preordersystem.Application.Users.RegisterCustomer;
using localmarket_preordersystem.Application.Users.RegisterProducer;
using Microsoft.Extensions.DependencyInjection;

namespace localmarket_preordersystem.Application
{
    /// <summary>
    /// A "config" mappa helyett/mellett ez egyetlen fájl regisztrálja az Application
    /// réteg saját osztályait (a Handler-eket). Az Infrastructure/api projekt hívja meg
    /// a Program.cs-ből: builder.Services.AddApplication(). Nem MediatR-alapú, mert
    /// egyelőre nincs rá konkrét szükség (nincs pipeline behavior, amit meg kellene
    /// oldani) — sima, explicit DI-regisztráció.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<RegisterCustomerHandler>();
            services.AddScoped<RegisterProducerHandler>();
            services.AddScoped<LoginHandler>();

            return services;
        }
    }
}