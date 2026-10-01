namespace localmarket_preordersystem.Application.Common.Exceptions
{
    /// <summary>
    /// 400 — bemeneti validációs hiba (pl. hibás e-mail formátum, túl rövid jelszó).
    /// Szándékosan külön típus a DomainException-től (üzleti szabálysértés): ez itt
    /// azelőtt bukik el, hogy bármilyen Domain objektum létrejönne (audit 20. pont:
    /// "Validation error" és "Domain rule violation" két külön hibaosztály).
    /// Nincs mögötte FluentValidation vagy más csomag — ennél a méretnél egy egyszerű,
    /// kézzel írt validátor osztály (ld. pl. RegisterCustomerValidator) elég.
    /// </summary>
    public class ValidationException(IReadOnlyDictionary<string, string[]> errors) : Exception("Egy vagy több mező érvénytelen.")
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
    }
}