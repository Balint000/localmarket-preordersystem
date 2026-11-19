namespace localmarket_preordersystem.Domain.Exceptions
{
    /// <summary>Üzleti szabály megsértése (API: 400). A domain invariánsai ezt dobják.</summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }
    }
}