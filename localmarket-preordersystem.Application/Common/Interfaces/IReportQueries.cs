namespace localmarket_preordersystem.Application.Common.Interfaces
{
    /// <summary>
    /// Riport-lekérdezések közvetlen DTO-projekcióval (összesítés az adatbázisban, nem aggregate-ek betöltésével).
    /// Infrastructure implementálja EF Core-ral; a hozzá tartozó DTO-k a Reports mappában vannak.
    /// </summary>
    public interface IReportQueries
    {
        Task<IReadOnlyList<Reports.TopProducts.TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, int top, CancellationToken cancellationToken = default);
        Task<Reports.CancellationRate.CancellationRateDto> GetCancellationRateAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Reports.VendorRevenue.VendorRevenueDto>> GetVendorRevenueAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Reports.UnclaimedOrders.UnclaimedOrderDto>> GetUnclaimedOrdersAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    }
}
