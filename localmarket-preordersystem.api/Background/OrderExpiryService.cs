using localmarket_preordersystem.Application.Orders.ExpirePendingOrders;

namespace localmarket_preordersystem.api.Background;

/// <summary>
/// Időszakosan lezárja azokat a függő rendeléseket, amelyekre az árus az átvételi idősáv végéig nem válaszolt
/// (állapot: Expired), és visszaadja a foglalt készletet és idősáv-helyet. Gyakoriság: Orders:ExpiryCheckMinutes.
/// Ha több API-példány fut, a párhuzamos futást a készlet/idősáv konkurenciatokenjei védik.
/// </summary>
public sealed class OrderExpiryService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<OrderExpiryService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = Math.Clamp(configuration.GetValue("Orders:ExpiryCheckMinutes", 15), 1, 24 * 60);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ExpirePendingOrdersHandler>();
            var expired = await handler.HandleAsync(ct: ct);
            if (expired > 0)
                logger.LogInformation("{Count} függő rendelés lejárt (az árus nem válaszolt az idősáv végéig).", expired);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // leállás
        }
        catch (Exception ex)
        {
            // pl. az adatbázis még nem elérhető vagy párhuzamos módosítás: a következő futás újrapróbálja
            logger.LogWarning(ex, "A függő rendelések lejáratása sikertelen, a következő futás újrapróbálja.");
        }
    }
}
