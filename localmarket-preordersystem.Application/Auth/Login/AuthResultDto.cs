namespace localmarket_preordersystem.Application.Auth.Login
{
    /// <summary>
    /// A Login use case válasza. Nem azonos az ITokenService AuthToken-jével — ez azt
    /// egészíti ki a hívó számára hasznos felhasználó-adatokkal (DTO a válaszhoz, ahogy
    /// kérted, hogy mindenhol legyenek).
    /// </summary>
    public sealed record AuthResultDto(
        string Token,
        DateTime ExpiresAtUtc,
        int UserId,
        string Role,
        int? ProducerId);
}