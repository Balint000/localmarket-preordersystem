using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Users
{
    /// <summary>Felhasználó nyilvános adatai (jelszó-hash soha nem kerül ki), árus esetén a profil összegzésével.</summary>
    public sealed record UserDto(
        int Id,
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        string Role,
        bool IsActive,
        DateTime CreatedAt,
        int? ProducerId,
        string? ProducerName,
        string? ProducerStatus)
    {
        public static UserDto From(User u) => new(
            u.Id,
            u.FirstName,
            u.LastName,
            u.Email,
            u.PhoneNumber,
            u.Role.ToString(),
            u.IsActive,
            u.CreatedAt,
            u.ProducerId,
            u.Producer?.Name,
            u.Producer?.Status.ToString());
    }
}
