using System.Text.Json;
using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using AppValidation = localmarket_preordersystem.Application.Common.Exceptions.ValidationException;

namespace localmarket_preordersystem.api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is OperationCanceledException && ctx.RequestAborted.IsCancellationRequested)
        {
            ctx.Response.StatusCode = 499;
            return true;
        }

        var (status, title) = ex switch
        {
            AppValidation => (400, "Érvénytelen adat"),
            DomainException => (400, "Üzleti szabály megsértése"),
            AuthenticationException => (401, "Hitelesítési hiba"),
            ForbiddenAccessException => (403, "Hozzáférés megtagadva"),
            NotFoundException => (404, "Nem található"),
            ConflictException => (409, "Ütközés"),
            ConcurrencyConflictException or DbUpdateConcurrencyException => (409, "Párhuzamos módosítás"),
            DbUpdateException { InnerException: PostgresException { SqlState: "23505" } } => (409, "Már létező adat"),
            DbUpdateException { InnerException: PostgresException { SqlState: "23514" or "23503" or "22001" or "22003" or "23502" } } => (400, "Érvénytelen adat"),
            DbUpdateException { InnerException: PostgresException { SqlState: "40001" or "40P01" } } => (409, "Párhuzamos módosítás"),
            // az EF a tranziens adatbázis-hibát InvalidOperationException-be csomagolja (belül NpgsqlException), ez valóban 503
            DbUpdateException or NpgsqlException or TimeoutException or InvalidOperationException { InnerException: NpgsqlException }
                => (503, "Az adatbázis átmenetileg nem elérhető"),
            JsonException or BadHttpRequestException => (400, "Hibás kérés"),
            HttpRequestException => (502, "Külső szolgáltatás hiba"),
            _ => (500, "Váratlan hiba")
        };

        if (status >= 500)
            logger.LogError(ex, "Kezeletlen hiba ({Status}). TraceId: {TraceId}", status, ctx.TraceIdentifier);
        else
            logger.LogWarning("Kezelt hiba {Status} {Type}: {Message}. TraceId: {TraceId}", status, ex.GetType().Name, ex.Message, ctx.TraceIdentifier);

        var exposeDetail = status < 500 && ex is not DbUpdateException;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exposeDetail ? ex.Message : null,
            Instance = ctx.Request.Path
        };
        problem.Extensions["traceId"] = ctx.TraceIdentifier;
        if (ex is AppValidation ve) problem.Extensions["errors"] = ve.Errors;

        ctx.Response.StatusCode = status;
        var written = await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            ProblemDetails = problem,
            Exception = ex
        });

        if (!written)
            await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
