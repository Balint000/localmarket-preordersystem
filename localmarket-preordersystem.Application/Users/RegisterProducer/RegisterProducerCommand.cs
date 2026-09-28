namespace localmarket_preordersystem.Application.Users.RegisterProducer
{
    public sealed record RegisterProducerCommand(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string PhoneNumber,
        string ProducerName,
        int MarketId,
        int StallNumber);
}