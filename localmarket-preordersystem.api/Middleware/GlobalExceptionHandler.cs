using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace localmarket_preordersystem.api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            Application.Common.Exceptions.ValidationException => (400, "Érvénytelen adat"),
            DomainException => (400, "Üzleti szabály megsértése"),
            AuthenticationException => (401, "Hitelesítési hiba"),
            ForbiddenAccessException => (403, "Hozzáférés megtagadva"),
            NotFoundException => (404, "Nem található"),
            ConflictException => (409, "Ütközés"),
            ConcurrencyConflictException => (409, "Párhuzamos módosítás"),
            _ => (500, "Váratlan hiba")
        };

        if (status == 500)
            logger.LogError(ex, "Kezeletlen hiba");

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == 500 ? null : ex.Message
        };

        if (ex is Application.Common.Exceptions.ValidationException ve)
            problem.Extensions["errors"] = ve.Errors;

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
