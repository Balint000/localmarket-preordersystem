using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.api.Middleware;
using localmarket_preordersystem.Application;
using localmarket_preordersystem.Application.Common.Time;
using localmarket_preordersystem.Application.Products;
using localmarket_preordersystem.Infrastructure;
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
builder.Services.AddApplication();
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
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
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
    await app.InitializeAsync();

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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
