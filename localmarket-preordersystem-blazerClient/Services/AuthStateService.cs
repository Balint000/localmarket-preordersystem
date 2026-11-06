using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace localmarket_preordersystem.Services;

/// <summary>
/// Központi hitelesítési állapot. A JWT-t a böngészőben a Data Protection által titkosított
/// ProtectedSessionStorage őrzi (frissítés után innen töltődik vissza), a körfolyamatban memóriában él.
/// A jogosultságot a backend kényszeríti; a kliens csak a megjelenítéshez olvassa ki a token claim-jeit.
/// </summary>
public sealed class AuthStateService(ProtectedSessionStorage storage) : AuthenticationStateProvider
{
    private const string StorageKey = "lm.auth";
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private string? _token;
    private DateTime _expiresAtUtc;
    private bool _loaded;

    public event Action? SessionExpired;

    public string? Token => IsValid ? _token : null;
    public int? UserId { get; private set; }
    public string? Role { get; private set; }
    public int? ProducerId { get; private set; }
    public string? Email { get; private set; }

    public bool IsAuthenticated => IsValid;
    private bool IsValid => !string.IsNullOrEmpty(_token) && _expiresAtUtc > DateTime.UtcNow;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureLoadedAsync();
        return new AuthenticationState(IsValid ? BuildPrincipal() : Anonymous);
    }

    public async Task SignInAsync(string token, DateTime expiresAtUtc)
    {
        Apply(token, expiresAtUtc);
        _loaded = true;
        try { await storage.SetAsync(StorageKey, new StoredSession(token, expiresAtUtc)); }
        catch (InvalidOperationException) { /* JS még nem elérhető: a memóriabeli állapot így is érvényes */ }
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal())));
    }

    public async Task SignOutAsync(bool expired = false)
    {
        var wasSignedIn = _token is not null;
        Clear();
        _loaded = true;
        try { await storage.DeleteAsync(StorageKey); }
        catch (InvalidOperationException) { }
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
        if (expired && wasSignedIn) SessionExpired?.Invoke();
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var stored = await storage.GetAsync<StoredSession>(StorageKey);
            if (stored.Success && stored.Value is { } s && s.ExpiresAtUtc > DateTime.UtcNow)
                Apply(s.Token, s.ExpiresAtUtc);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Security.Cryptography.CryptographicException)
        {
            // JS még nem elérhető, vagy a tárolt adat nem olvasható (pl. újraindult a szerver): vendégként indulunk
            _loaded = ex is not InvalidOperationException;
        }
    }

    private void Apply(string token, DateTime expiresAtUtc)
    {
        _token = token;
        _expiresAtUtc = expiresAtUtc;
        var claims = ReadClaims(token);
        UserId = int.TryParse(claims.GetValueOrDefault("sub"), out var id) ? id : null;
        Role = claims.GetValueOrDefault("role");
        ProducerId = int.TryParse(claims.GetValueOrDefault("producer_id"), out var pid) ? pid : null;
        Email = claims.GetValueOrDefault("email");
    }

    private void Clear()
    {
        _token = null;
        _expiresAtUtc = default;
        UserId = null; Role = null; ProducerId = null; Email = null;
    }

    private ClaimsPrincipal BuildPrincipal()
    {
        var claims = new List<Claim>();
        if (UserId is int id) claims.Add(new Claim(ClaimTypes.NameIdentifier, id.ToString()));
        if (Email is not null) claims.Add(new Claim(ClaimTypes.Name, Email));
        if (Role is not null) claims.Add(new Claim(ClaimTypes.Role, Role));
        if (ProducerId is int pid) claims.Add(new Claim("producer_id", pid.ToString()));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
    }

    /// <summary>A token payloadjának kiolvasása ALÁÍRÁS-ELLENŐRZÉS NÉLKÜL — kizárólag megjelenítéshez; a szerver minden kérésnél validál.</summary>
    private static Dictionary<string, string> ReadClaims(string token)
    {
        var result = new Dictionary<string, string>();
        var parts = token.Split('.');
        if (parts.Length < 2) return result;
        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            foreach (var p in doc.RootElement.EnumerateObject())
                if (p.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    result[p.Name] = p.Value.ToString();
        }
        catch (Exception ex) when (ex is FormatException or JsonException) { }
        return result;
    }

    private sealed record StoredSession(string Token, DateTime ExpiresAtUtc);
}
