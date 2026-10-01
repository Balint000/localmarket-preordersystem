namespace localmarket_preordersystem.Application.Common.Exceptions
{
    /// <summary>
    /// 403 — a hívó be van jelentkezve, de nincs jogosultsága a művelethez (pl. másik
    /// árus termékét próbálja módosítani). Szándékosan külön típus a DomainException-től:
    /// az audit 20. pontja szerint az authorization és a domain invariant két különböző
    /// hibaosztály, más HTTP státuszra kell fordulniuk (403 vs. 400).
    /// </summary>
    public class ForbiddenAccessException(string message) : Exception(message)
    {
    }
}