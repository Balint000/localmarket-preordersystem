namespace localmarket_preordersystem.Application.Common.Exceptions
{
    /// <summary>409 — pl. már foglalt e-mail cím regisztrációkor.</summary>
    public class ConflictException(string message) : Exception(message)
    {
    }
}