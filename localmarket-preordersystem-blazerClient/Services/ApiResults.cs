namespace localmarket_preordersystem.Services;

public enum ApiFailure { None, Validation, Unauthorized, Forbidden, NotFound, Conflict, BadRequest, Server, Network }

/// <summary>A szerver ProblemDetails válaszának kliensoldali alakja.</summary>
public sealed record ApiProblem(ApiFailure Kind, string Title, string? Detail, IReadOnlyDictionary<string, string[]> Errors)
{
    public string Message => !string.IsNullOrWhiteSpace(Detail) ? Detail! : Title;
}

/// <summary>Egységes eredmény: siker vagy hiba, kivétel nélkül (a komponensek ebből döntenek).</summary>
public sealed class ApiResult<T>
{
    public T? Value { get; }
    public ApiProblem? Problem { get; }
    public bool IsSuccess => Problem is null;

    private ApiResult(T? value, ApiProblem? problem) { Value = value; Problem = problem; }

    public static ApiResult<T> Ok(T value) => new(value, null);
    public static ApiResult<T> Fail(ApiProblem problem) => new(default, problem);
}

/// <summary>Visszatérési érték nélküli műveletekhez.</summary>
public readonly record struct Unit;
