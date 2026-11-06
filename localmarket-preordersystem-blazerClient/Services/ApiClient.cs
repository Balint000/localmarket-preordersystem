using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace localmarket_preordersystem.Services;

/// <summary>
/// Az egyetlen hely, ahol HTTP-hívás történik. Felel a Bearer fejlécért, és a hibákat (400/401/403/404/409/5xx,
/// hálózat, időtúllépés) egységes <see cref="ApiProblem"/>-mé alakítja. 401-nél kijelentkeztet.
/// </summary>
public sealed class ApiClient(HttpClient http, AuthStateService auth)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Get, url, null, ct);
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Post, url, body, ct);
    public Task<ApiResult<T>> PutAsync<T>(string url, object? body, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Put, url, body, ct);
    public Task<ApiResult<T>> DeleteAsync<T>(string url, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Delete, url, null, ct);

    /// <summary>Fájl (export) letöltése bájtokként.</summary>
    public async Task<ApiResult<DownloadedFile>> GetFileAsync(string url, CancellationToken ct = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, url, null);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return ApiResult<DownloadedFile>.Fail(await ToProblemAsync(response, ct));

            var name = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "export";
            var type = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return ApiResult<DownloadedFile>.Ok(new DownloadedFile(name, type, await response.Content.ReadAsByteArrayAsync(ct)));
        }
        catch (Exception ex) when (IsNetworkError(ex, ct))
        {
            return ApiResult<DownloadedFile>.Fail(NetworkProblem(ex));
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        try
        {
            using var request = CreateRequest(method, url, body);
            using var response = await http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ToProblemAsync(response, ct);
                if (problem.Kind == ApiFailure.Unauthorized && auth.IsAuthenticated)
                    await auth.SignOutAsync(expired: true);
                return ApiResult<T>.Fail(problem);
            }

            if (typeof(T) == typeof(Unit) || response.StatusCode == HttpStatusCode.NoContent)
                return ApiResult<T>.Ok(default!);

            var value = await response.Content.ReadFromJsonAsync<T>(Json, ct);
            return value is null
                ? ApiResult<T>.Fail(new ApiProblem(ApiFailure.Server, "Üres válasz érkezett a szervertől.", null, Empty))
                : ApiResult<T>.Ok(value);
        }
        catch (Exception ex) when (IsNetworkError(ex, ct))
        {
            return ApiResult<T>.Fail(NetworkProblem(ex));
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? body)
    {
        var request = new HttpRequestMessage(method, url);
        if (auth.Token is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        return request;
    }

    private static readonly IReadOnlyDictionary<string, string[]> Empty = new Dictionary<string, string[]>();

    private static async Task<ApiProblem> ToProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var kind = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => ApiFailure.BadRequest,
            HttpStatusCode.Unauthorized => ApiFailure.Unauthorized,
            HttpStatusCode.Forbidden => ApiFailure.Forbidden,
            HttpStatusCode.NotFound => ApiFailure.NotFound,
            HttpStatusCode.Conflict => ApiFailure.Conflict,
            HttpStatusCode.TooManyRequests => ApiFailure.Server,
            >= HttpStatusCode.InternalServerError => ApiFailure.Server,
            _ => ApiFailure.BadRequest
        };

        string? title = null, detail = null;
        var errors = Empty;
        try
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
                    detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;
                    if (root.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
                    {
                        errors = e.EnumerateObject().ToDictionary(
                            p => p.Name,
                            p => p.Value.EnumerateArray().Select(x => x.GetString() ?? "").ToArray());
                        if (errors.Count > 0) kind = ApiFailure.Validation;
                    }
                }
            }
        }
        catch (JsonException) { }

        title ??= kind switch
        {
            ApiFailure.Unauthorized => "A munkamenet lejárt vagy nem vagy bejelentkezve.",
            ApiFailure.Forbidden => "Ehhez a művelethez nincs jogosultságod.",
            ApiFailure.NotFound => "A kért elem nem található.",
            ApiFailure.Conflict => "Az adat közben megváltozott, töltsd újra az oldalt.",
            ApiFailure.Server => response.StatusCode == HttpStatusCode.TooManyRequests
                ? "Túl sok kérés, próbáld újra egy perc múlva."
                : "Szerverhiba történt, próbáld újra később.",
            _ => "A kérés nem teljesíthető."
        };
        return new ApiProblem(kind, title, detail, errors);
    }

    private static bool IsNetworkError(Exception ex, CancellationToken ct)
        => ex is HttpRequestException || (ex is TaskCanceledException && !ct.IsCancellationRequested);

    private static ApiProblem NetworkProblem(Exception ex) => new(
        ApiFailure.Network,
        ex is TaskCanceledException ? "A szerver nem válaszolt időben." : "A szerver nem érhető el. Ellenőrizd a kapcsolatot.",
        null, Empty);
}

public sealed record DownloadedFile(string FileName, string ContentType, byte[] Content);
