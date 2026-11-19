namespace localmarket_preordersystem.Application.Users.ChangeMyEmail
{
    public sealed record ChangeMyEmailCommand(string NewEmail, string CurrentPassword);
}
