using System;
using System.Collections.Generic;
using System.Text;

namespace localmarket_preordersystem.Application.Common.Models
{
    /// <summary>Az ITokenService kimenete — nem a végleges API-válasz DTO-ja, azt a Login
    /// use case AuthResultDto-ja adja, ami ezt egészíti ki felhasználó-adatokkal.</summary>
    public sealed record AuthToken(string Value, DateTime ExpiresAtUtc);
}
