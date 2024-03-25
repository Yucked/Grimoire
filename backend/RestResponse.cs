namespace Grimoire;

/// <summary>
/// 
/// </summary>
/// <param name="StatusCode"></param>
/// <param name="Data"></param>
public record RestResponse(int StatusCode, object Data = null!) {
    public static RestResponse New(int statusCode, object data = null!)
        => new(statusCode, data);
}