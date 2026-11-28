using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Common.Models
{
    /// <summary>
    /// A felhasználó jogosultság szempontjából lényeges, aktuális adatai. Minden hitelesített kérésnél
    /// ezzel vetjük össze a token claim-jeit (letiltott fiók, megváltozott szerepkör → a token érvénytelen).
    /// </summary>
    public sealed record UserAccessInfo(bool IsActive, Role Role, int? ProducerId);
}
