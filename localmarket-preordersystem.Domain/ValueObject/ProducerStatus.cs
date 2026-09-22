namespace localmarket_preordersystem.Domain.ValueObject
{
    /// <summary>
    /// Termelő státusz enum, tulajdonos hagyja jóvá a termelőket.
    /// </summary>
    public enum ProducerStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
    }
}