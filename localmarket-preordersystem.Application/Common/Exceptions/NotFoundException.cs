namespace localmarket_preordersystem.Application.Common.Exceptions
{
    /// <summary>404 — a kért erőforrás nem létezik. Nem Domain-kivétel: ez egy Application/
    /// lekérdezési szintű állapot, nem üzleti szabálysértés.</summary>
    public class NotFoundException(string message) : Exception(message)
    {
    }
}