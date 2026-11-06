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
