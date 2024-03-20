namespace Grimoire;

/// <summary>
/// 
/// </summary>
/// <param name="StatusCode"></param>
/// <param name="Data"></param>
/// <typeparam name="T"></typeparam>
public record Response<T>(int StatusCode, T Data = default) {
    public static Response<T> New(int statusCode, T data) => new(statusCode, data);
}