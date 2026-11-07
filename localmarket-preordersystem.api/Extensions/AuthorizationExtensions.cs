using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.api.Extensions;

public static class Policies
{
    public const string Customer = nameof(Customer);
    public const string Producer = nameof(Producer);
    public const string Manager = nameof(Manager);
    public const string ProducerOrManager = nameof(ProducerOrManager);
}

public static class AuthorizationExtensions
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Customer, p => p.RequireRole(nameof(Role.Customer)))
            .AddPolicy(Policies.Producer, p => p.RequireRole(nameof(Role.Producer)))
            .AddPolicy(Policies.Manager, p => p.RequireRole(nameof(Role.Admin)))
            .AddPolicy(Policies.ProducerOrManager, p => p.RequireRole(nameof(Role.Producer), nameof(Role.Admin)));
        return services;
    }
}
