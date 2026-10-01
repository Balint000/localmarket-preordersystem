namespace localmarket_preordersystem.Application.Users.RegisterCustomer
{
    public sealed record RegisterCustomerCommand(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string PhoneNumber);
}