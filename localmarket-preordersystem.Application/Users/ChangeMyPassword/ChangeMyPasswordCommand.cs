namespace localmarket_preordersystem.Application.Users.ChangeMyPassword
{
    public sealed record ChangeMyPasswordCommand(string CurrentPassword, string NewPassword);
}
