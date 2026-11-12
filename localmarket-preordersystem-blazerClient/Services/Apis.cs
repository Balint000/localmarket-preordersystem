using localmarket_preordersystem.Models;

namespace localmarket_preordersystem.Services;

// Vékony, típusos kliensek a végpontok fölött. A Razor komponensek csak ezeket hívják.

public sealed class AuthApi(ApiClient api)
{
    public Task<ApiResult<AuthResult>> LoginAsync(LoginRequest r, CancellationToken ct = default) => api.PostAsync<AuthResult>("api/auth/login", r, ct);
    public Task<ApiResult<RegisteredResult>> RegisterCustomerAsync(RegisterCustomerRequest r, CancellationToken ct = default) => api.PostAsync<RegisteredResult>("api/auth/register-customer", r, ct);
    public Task<ApiResult<RegisteredResult>> RegisterProducerAsync(RegisterProducerRequest r, CancellationToken ct = default) => api.PostAsync<RegisteredResult>("api/auth/register-producer", r, ct);
}

public sealed class CatalogApi(ApiClient api)
{
    public Task<ApiResult<IReadOnlyList<MarketInfo>>> MarketsAsync(CancellationToken ct = default) => api.GetAsync<IReadOnlyList<MarketInfo>>("api/markets", ct);
    public Task<ApiResult<IReadOnlyList<CategoryInfo>>> CategoriesAsync(CancellationToken ct = default) => api.GetAsync<IReadOnlyList<CategoryInfo>>("api/categories", ct);

    public Task<ApiResult<IReadOnlyList<ProductInfo>>> SearchProductsAsync(string? text, int? categoryId, IEnumerable<string> excludeAllergies, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(text)) query.Add("text=" + Uri.EscapeDataString(text.Trim()));
        if (categoryId is int c) query.Add("categoryId=" + c);
        query.AddRange(excludeAllergies.Select(a => "excludeAllergies=" + Uri.EscapeDataString(a)));
        var url = "api/products" + (query.Count > 0 ? "?" + string.Join('&', query) : "");
        return api.GetAsync<IReadOnlyList<ProductInfo>>(url, ct);
    }

    public Task<ApiResult<ProductInfo>> ProductAsync(int id, CancellationToken ct = default) => api.GetAsync<ProductInfo>($"api/products/{id}", ct);
    public Task<ApiResult<IReadOnlyList<ProductInfo>>> ProductsOfProducerAsync(int producerId, CancellationToken ct = default) => api.GetAsync<IReadOnlyList<ProductInfo>>($"api/products?producerId={producerId}", ct);
    public Task<ApiResult<IReadOnlyList<ProducerInfo>>> ProducersAsync(string? status = null, int? marketId = null, CancellationToken ct = default)
    {
        var q = new List<string>();
        if (!string.IsNullOrEmpty(status)) q.Add("status=" + Uri.EscapeDataString(status));
        if (marketId is int m) q.Add("marketId=" + m);
        return api.GetAsync<IReadOnlyList<ProducerInfo>>("api/producers" + (q.Count > 0 ? "?" + string.Join('&', q) : ""), ct);
    }
    public Task<ApiResult<MarketWeatherInfo>> WeatherAsync(int marketId, DateTime date, CancellationToken ct = default) => api.GetAsync<MarketWeatherInfo>($"api/weather/markets/{marketId}?date={date:yyyy-MM-dd}", ct);
    public Task<ApiResult<IReadOnlyList<PickupSlotInfo>>> PickupSlotsAsync(int producerId, CancellationToken ct = default) => api.GetAsync<IReadOnlyList<PickupSlotInfo>>($"api/pickup-slots?producerId={producerId}", ct);
}

/// <summary>Kosár-API és a navbar számlálóját tápláló megosztott állapot.</summary>
public sealed class CartService(ApiClient api)
{
    public event Action? Changed;
    public int ItemCount { get; private set; }

    public async Task<ApiResult<CartInfo>> GetAsync(CancellationToken ct = default) => Track(await api.GetAsync<CartInfo>("api/cart", ct));
    public async Task<ApiResult<CartInfo>> AddAsync(int productId, decimal quantity, CancellationToken ct = default) => Track(await api.PostAsync<CartInfo>("api/cart/items", new AddToCartRequest(productId, quantity), ct));
    public async Task<ApiResult<CartInfo>> SetQuantityAsync(int productId, decimal quantity, CancellationToken ct = default) => Track(await api.PutAsync<CartInfo>($"api/cart/items/{productId}", new QuantityRequest(quantity), ct));
    public async Task<ApiResult<CartInfo>> RemoveAsync(int productId, CancellationToken ct = default) => Track(await api.DeleteAsync<CartInfo>($"api/cart/items/{productId}", ct));

    public void Reset() { ItemCount = 0; Changed?.Invoke(); }

    private ApiResult<CartInfo> Track(ApiResult<CartInfo> result)
    {
        if (result is { IsSuccess: true, Value: { } cart }) { ItemCount = cart.ItemCount; Changed?.Invoke(); }
        return result;
    }
}

public sealed class OrdersApi(ApiClient api, CartService cart)
{
    public async Task<ApiResult<IReadOnlyList<OrderInfo>>> PlaceAsync(PlaceOrderRequest r, CancellationToken ct = default)
    {
        var result = await api.PostAsync<IReadOnlyList<OrderInfo>>("api/orders", r, ct);
        if (result.IsSuccess) cart.Reset(); // a szerver a rendelés után kiüríti a kosarat
        return result;
    }

    public Task<ApiResult<IReadOnlyList<OrderInfo>>> MineAsync(string? status, CancellationToken ct = default) => api.GetAsync<IReadOnlyList<OrderInfo>>("api/orders/my" + StatusQuery(status), ct);
    public Task<ApiResult<IReadOnlyList<OrderInfo>>> ForProducerAsync(string? status, CancellationToken ct = default) => api.GetAsync<IReadOnlyList<OrderInfo>>("api/orders/producer" + StatusQuery(status), ct);
    public Task<ApiResult<OrderInfo>> GetAsync(int id, CancellationToken ct = default) => api.GetAsync<OrderInfo>($"api/orders/{id}", ct);

    public Task<ApiResult<OrderInfo>> CancelAsync(int id) => api.PostAsync<OrderInfo>($"api/orders/{id}/cancel");
    public Task<ApiResult<OrderInfo>> AcceptAsync(int id) => api.PostAsync<OrderInfo>($"api/orders/{id}/accept");
    public Task<ApiResult<OrderInfo>> AcceptPartiallyAsync(int id, IReadOnlyList<FulfilledLineRequest> lines) => api.PostAsync<OrderInfo>($"api/orders/{id}/accept-partially", new PartialRequest(lines));
    public Task<ApiResult<OrderInfo>> RejectAsync(int id, string reason) => api.PostAsync<OrderInfo>($"api/orders/{id}/reject", new ReasonRequest(reason));
    public Task<ApiResult<OrderInfo>> ReadyAsync(int id) => api.PostAsync<OrderInfo>($"api/orders/{id}/ready");
    public Task<ApiResult<OrderInfo>> PickedUpAsync(int id) => api.PostAsync<OrderInfo>($"api/orders/{id}/picked-up");
    public Task<ApiResult<OrderInfo>> NotPickedUpAsync(int id) => api.PostAsync<OrderInfo>($"api/orders/{id}/not-picked-up");
    public Task<ApiResult<IReadOnlyList<OrderInfo>>> DisputedAsync(CancellationToken ct = default) => api.GetAsync<IReadOnlyList<OrderInfo>>("api/orders/disputed", ct);
    public Task<ApiResult<OrderInfo>> ResolveDisputeAsync(int id, string decision, string? note) => api.PostAsync<OrderInfo>($"api/orders/{id}/resolve-dispute", new ResolveRequest(decision, note));
    public Task<ApiResult<OrderInfo>> DisputeAsync(int id, string note) => api.PostAsync<OrderInfo>($"api/orders/{id}/dispute", new NoteRequest(note));

    public Task<ApiResult<DownloadedFile>> ExportOrderAsync(int id, string format) => api.GetFileAsync($"api/export/orders/{id}?format={format}");
    public Task<ApiResult<DownloadedFile>> ExportPickingListAsync(DateTime date, string format) => api.GetFileAsync($"api/export/picking-list?date={date:yyyy-MM-dd}&format={format}");

    private static string StatusQuery(string? status) => string.IsNullOrEmpty(status) ? "" : "?status=" + Uri.EscapeDataString(status);
}

/// <summary>Árus-oldali műveletek: termék, készlet, átvételi időpont, saját adatok.</summary>
public sealed class VendorApi(ApiClient api)
{
    public Task<ApiResult<ProductInfo>> CreateProductAsync(CreateProductRequest r) => api.PostAsync<ProductInfo>("api/products", r);
    public Task<ApiResult<ProductInfo>> UpdateProductAsync(int id, UpdateProductRequest r) => api.PutAsync<ProductInfo>($"api/products/{id}", r, default);
    public Task<ApiResult<ProductInfo>> SetStockAsync(int id, StockRequest r) => api.PutAsync<ProductInfo>($"api/products/{id}/stock", r, default);
    public Task<ApiResult<PickupSlotInfo>> CreatePickupSlotAsync(CreatePickupSlotRequest r) => api.PostAsync<PickupSlotInfo>("api/pickup-slots", r);
    public Task<ApiResult<ProducerInfo>> UpdateMineAsync(UpdateMyProducerRequest r) => api.PutAsync<ProducerInfo>("api/producers/me", r, default);
}

/// <summary>Admin-műveletek: árusok jóváhagyása, piacok, kategóriák, riportok.</summary>
public sealed class AdminApi(ApiClient api)
{
    public Task<ApiResult<ProducerInfo>> ApproveProducerAsync(int id) => api.PostAsync<ProducerInfo>($"api/producers/{id}/approve");
    public Task<ApiResult<ProducerInfo>> RejectProducerAsync(int id, string reason) => api.PostAsync<ProducerInfo>($"api/producers/{id}/reject", new ReasonRequest(reason));
    public Task<ApiResult<MarketInfo>> CreateMarketAsync(CreateMarketRequest r) => api.PostAsync<MarketInfo>("api/markets", r);
    public Task<ApiResult<MarketInfo>> UpdateMarketAsync(int id, UpdateMarketRequest r) => api.PutAsync<MarketInfo>($"api/markets/{id}", r, default);
    public Task<ApiResult<CategoryInfo>> CreateCategoryAsync(CreateCategoryRequest r) => api.PostAsync<CategoryInfo>("api/categories", r);

    public Task<ApiResult<IReadOnlyList<TopProductInfo>>> TopProductsAsync(DateTime? from, DateTime? to, int top = 10) => api.GetAsync<IReadOnlyList<TopProductInfo>>("api/reports/top-products" + Range(from, to, "top=" + top), default);
    public Task<ApiResult<CancellationRateInfo>> CancellationRateAsync(DateTime? from, DateTime? to) => api.GetAsync<CancellationRateInfo>("api/reports/cancellation-rate" + Range(from, to), default);
    public Task<ApiResult<IReadOnlyList<VendorRevenueInfo>>> VendorRevenueAsync(DateTime? from, DateTime? to) => api.GetAsync<IReadOnlyList<VendorRevenueInfo>>("api/reports/vendor-revenue" + Range(from, to), default);
    public Task<ApiResult<IReadOnlyList<UnclaimedOrderInfo>>> UnclaimedAsync(DateTime? from, DateTime? to) => api.GetAsync<IReadOnlyList<UnclaimedOrderInfo>>("api/reports/unclaimed-orders" + Range(from, to), default);

    private static string Range(DateTime? from, DateTime? to, string? extra = null)
    {
        var q = new List<string>();
        if (from is { } f) q.Add($"from={f:yyyy-MM-dd}");
        if (to is { } t) q.Add($"to={t:yyyy-MM-dd}");
        if (extra is not null) q.Add(extra);
        return q.Count > 0 ? "?" + string.Join('&', q) : "";
    }
}
