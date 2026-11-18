using localmarket_preordersystem.Models;

namespace localmarket_preordersystem.Services;

/// <summary>A bejelentkezett felhasználó saját profilja (api/me).</summary>
public sealed class ProfileApi(ApiClient api)
{
    public Task<ApiResult<UserInfo>> GetAsync(CancellationToken ct = default) => api.GetAsync<UserInfo>("api/me", ct);
    public Task<ApiResult<UserInfo>> UpdateAsync(UpdateProfileRequest r) => api.PutAsync<UserInfo>("api/me", r, default);
    public Task<ApiResult<Unit>> ChangePasswordAsync(ChangePasswordRequest r) => api.PostAsync<Unit>("api/me/password", r);
}
