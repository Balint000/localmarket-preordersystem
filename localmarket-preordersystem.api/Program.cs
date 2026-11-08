using System.Text.Json.Serialization;
using localmarket_preordersystem.api.Extensions;
using localmarket_preordersystem.api.Middleware;
using localmarket_preordersystem.Application;
using localmarket_preordersystem.Application.Products;
using localmarket_preordersystem.Infrastructure;
using localmarket_preordersystem.Infrastructure.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiAuth();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
