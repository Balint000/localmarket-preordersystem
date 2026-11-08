using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;

namespace localmarket_preordersystem.Application.Users.GetMyProfile
{
    /// <summary>A bejelentkezett felhasználó saját adatai — szerepkörtől és (árusnál) jóváhagyási státusztól függetlenül.</summary>
    public sealed class GetMyProfileHandler(IUserRepository users, ICurrentUserService currentUser)
    {
        public async Task<UserDto> HandleAsync(CancellationToken ct = default)
        {
            var userId = currentUser.RequireUserId();
            var user = await users.GetByIdAsync(userId, ct)
                ?? throw new NotFoundException("A felhasználó nem található.");
            return UserDto.From(user);
        }
    }
}
