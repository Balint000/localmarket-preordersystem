namespace localmarket_preordersystem.Models;

public sealed record UserInfo(
    int Id, string FirstName, string LastName, string Email, string PhoneNumber, string Role,
    bool IsActive, DateTime CreatedAt, int? ProducerId, string? ProducerName, string? ProducerStatus);

public sealed record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ChangeEmailRequest(string NewEmail, string CurrentPassword);
