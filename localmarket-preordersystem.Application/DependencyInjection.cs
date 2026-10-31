using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace localmarket_preordersystem.Application
{
    /// <summary>
    /// Az Application réteg regisztrációja. Minden "*Handler" nevű, nem absztrakt osztályt
    /// automatikusan scoped-ként vesz fel (új use case-nél nem kell ide nyúlni). Nem MediatR:
    /// a controller közvetlenül a konkrét handlert kapja injektálva.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.TryAddSingleton(TimeProvider.System);

            var handlerTypes = typeof(DependencyInjection).Assembly
                .GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Handler", StringComparison.Ordinal));

            foreach (var type in handlerTypes)
                services.AddScoped(type);

            return services;
        }
    }
}
