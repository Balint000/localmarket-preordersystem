using localmarket_preordersystem.Components;
using localmarket_preordersystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// hitelesítési állapot: egy közös szolgáltatás, amit az AuthenticationStateProvider is használ
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, PassThroughAuthorizationHandler>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateService>());

// az API címe konfigurációból jön (Api:BaseUrl), docker-compose-ban: Api__BaseUrl
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl) || !Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBaseUri))
{
    throw new InvalidOperationException("Az 'Api:BaseUrl' konfiguráció hiányzik vagy érvénytelen (pl. http://localhost:5205). Állítsd be az appsettings.Development.json-ban vagy az Api__BaseUrl környezeti változóval.");
}

// a böngésző által elérhető cím (képekhez); alapértelmezés: ugyanaz, mint az Api:BaseUrl
var publicBaseUrl = builder.Configuration["Api:PublicBaseUrl"];
builder.Services.AddSingleton(new ApiUrls(string.IsNullOrWhiteSpace(publicBaseUrl) ? apiBaseUri.ToString() : publicBaseUrl));

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUri.ToString().TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<FileDownloader>();
builder.Services.AddScoped<AuthApi>();
builder.Services.AddScoped<ProfileApi>();
builder.Services.AddScoped<CatalogApi>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrdersApi>();
builder.Services.AddScoped<VendorApi>();
builder.Services.AddScoped<AdminApi>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
