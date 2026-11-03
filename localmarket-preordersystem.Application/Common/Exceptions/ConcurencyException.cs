namespace localmarket_preordersystem.Application.Common.Exceptions
{
    /// <summary>409 — egyszerre módosított adat (pl. két vevő az utolsó darabot / idősáv-helyet foglalja).
    /// Az IUnitOfWork implementációja fordítja ide az EF Core DbUpdateConcurrencyException-t.</summary>
    public class ConcurrencyConflictException(string message) : Exception(message)
    {
    }
}