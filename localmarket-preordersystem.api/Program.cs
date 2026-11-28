using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using localmarket_preordersystem.api.Background;
using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.api.Middleware;
using localmarket_preordersystem.Application;
using localmarket_preordersystem.Application.Common.Time;
using localmarket_preordersystem.Application.Orders;
using localmarket_preordersystem.Application.Products;
using localmarket_preordersystem.Infrastructure;
using localmarket_preordersystem.Infrastructure.Auth;
using localmarket_preordersystem.Infrastructure.Storage;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
// A "ma" és az idősávok időpontjai a piac helyi idejében értendők (a konténer UTC-ben fut),
// ezért a TimeProvider az App:TimeZone időzónát használja. Az AddApplication TryAdd-ja nem írja felül.
builder.Services.AddSingleton<TimeProvider>(ZonedTimeProvider.Create(builder.Configuration["App:TimeZone"] ?? "Europe/Budapest"));
builder.Services.AddSingleton(new OrderPolicy(
    TimeSpan.FromHours(Math.Clamp(builder.Configuration.GetValue("Orders:CustomerCancellationCutoffHours", 12), 0, 7 * 24))));
builder.Services.AddApplication();
if (builder.Configuration.GetValue("Orders:ExpiryEnabled", true))
    builder.Services.AddHostedService<OrderExpiryService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiAuth();

// Kéréskorlátozás. Figyelem: a Blazor Server kliens szerver oldalról hívja az API-t, ezért az összes
// felhasználó ugyanarról az IP-ről érkezik, így a limit gyakorlatilag globális. Emiatt a határok
// szándékosan bőkezűek, és konfigurálhatók (RateLimit:*).
var generalPerMinute = builder.Configuration.GetValue("RateLimit:GeneralPerMinute", 1200);
var authPerMinute = builder.Configuration.GetValue("RateLimit:AuthPerMinute", 60);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Bejelentkezett kérésnél felhasználónként, egyébként IP szerint számolunk. A Blazor Server kliens
    // miatt minden kérés ugyanarról az IP-ről jön, így az IP-alapú kulcs csak a vendégforgalomra marad.
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.FindFirst(JwtClaimNames.UserId)?.Value is { } userId
                ? "user:" + userId
                : "ip:" + (ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = generalPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    var startupOk = false;
    for (var attempt = 1; attempt <= 5 && !startupOk; attempt++)
    {
        try
        {
            await app.InitializeAsync();
            startupOk = true;
        }
        catch (Exception ex) when (attempt < 5)
        {
            app.Logger.LogWarning(ex, "Adatbázis-inicializálás sikertelen ({Attempt}/5), újrapróbálás.", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3 * attempt));
        }
        catch (Exception ex)
        {
            app.Logger.LogCritical(ex, "Adatbázis-inicializálás véglegesen sikertelen, a szolgáltatás leáll.");
            return 1;
        }
    }
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseHttpsRedirection();

// Feltöltött termékképek kiszolgálása (a gyökérmappa az ImageStorage:RootPath-ból jön).
var imageRoot = Path.GetFullPath(app.Services.GetRequiredService<IOptions<ImageStorageOptions>>().Value.RootPath);
Directory.CreateDirectory(imageRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imageRoot),
    RequestPath = ProductImages.PublicPath,
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.XContentTypeOptions = "nosniff";
        ctx.Context.Response.Headers.CacheControl = "public,max-age=86400"; // a fájlnév egyedi (GUID), csere = új URL
    }
});

app.UseCors();
app.UseAuthentication();
app.UseRateLimiter(); // a hitelesítés után, hogy a felhasználó szerinti kulcs elérhető legyen
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
return 0;
