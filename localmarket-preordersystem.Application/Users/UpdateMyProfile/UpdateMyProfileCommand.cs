namespace localmarket_preordersystem.Application.Users.UpdateMyProfile
{
    public sealed record UpdateMyProfileCommand(string FirstName, string LastName, string? PhoneNumber);
}
