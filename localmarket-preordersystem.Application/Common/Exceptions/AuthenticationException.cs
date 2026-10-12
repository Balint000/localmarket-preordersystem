namespace localmarket_preordersystem.Application.Common.Exceptions
{
    /// <summary>401 — hibás belépési adatok vagy inaktív felhasználó. Szándékosan ugyanaz
    /// az üzenet érvénytelen e-mailre és hibás jelszóra is (ne lehessen a hibaüzenetből
    /// kitalálni, hogy egy e-mail cím regisztrálva van-e).</summary>
    public class AuthenticationException(string message) : Exception(message)
    {
    }
}