using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Interfaces
{
    public interface ITokenService
    {
        /// <summary>
        /// JWT előállítása a felhasználóhoz. A tokenbe kerülő claim-ek (userId, role,
        /// producerId ha van) teszik lehetővé, hogy az ICurrentUserService adatbázis-hívás
        /// nélkül, közvetlenül a tokenből tudja kiolvasni a hívó jogosultságait.
        /// </summary>
        AuthToken GenerateToken(User user);

    }
}
