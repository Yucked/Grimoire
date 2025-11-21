using System.Text;
using Grimoire.Objects;
using Microsoft.Playwright;

namespace Grimoire;
public static class Extensions {
    public static ResponseObject AsResponse(this object @object, int statusCode)
        => ResponseObject.New(statusCode, @object);

    public static ValueTask<ResponseObject> AsResponseAsync(this object @object, int statusCode)
        => ValueTask.FromResult(ResponseObject.New(statusCode, @object));

    public static string ToId(this string @string)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(@string));

    public static string ToText(this string @string)
        => Encoding.UTF8.GetString(Convert.FromBase64String(@string));

    public static async Task<IReadOnlyList<T>> AwaitAsync<T>(this IEnumerable<Task<T>> tasks)
        => await Task.WhenAll(tasks);

    public static async Task<string> GetTextContentAsync(this Task<IElementHandle> element) {
        var elm = await element;
        return (await elm.TextContentAsync())!;
    }

    public static async Task<string> GetAttributeAsync(this Task<IElementHandle> element, string attribute) {
        var elm = await element;
        return (await elm.GetAttributeAsync(attribute))!;
    }

    // ReSharper disable once InconsistentNaming
    public static IDictionary<string, string> AsKV(this Exception exception) {
        return exception
            .GetType()
            .GetProperties()
            .Select(x => new {
                x.Name,
                Value = $"{x.GetValue(exception, null)}"
            })
            .ToDictionary(k => k.Name, v => v.Value);
    }
}