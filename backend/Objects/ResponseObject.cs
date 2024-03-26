namespace Grimoire.Objects;

/// <summary>
/// 
/// </summary>
/// <param name="StatusCode"></param>
/// <param name="Data"></param>
public record ResponseObject(int StatusCode, object Data = null!) {
    public static ResponseObject New(int statusCode, object data = null!)
        => new(statusCode, data);
}