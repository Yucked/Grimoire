using System.Text;
using Grimoire.Commons.Parsing;
using Grimoire.Commons.Proxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grimoire.Commons;

public static class Extensions {
    public static string GetIdFromName(this string name) {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(name));
    }

    public static string GetNameFromId(this string id) {
        return Encoding.UTF8.GetString(Convert.FromBase64String(id));
    }

    public static T RandomItem<T>(this T[] items) {
        return items[Random.Shared.Next(items.Length - 1)];
    }

    public static T RandomItem<T>(this IReadOnlyList<T> items) {
        return items[Random.Shared.Next(items.Count - 1)];
    }

    public static IServiceCollection AddHtmlParser(this IServiceCollection collection, ParserOptions options) {
        return collection
            .AddSingleton<ProxiesHandler>()
            .AddSingleton(x => new HtmlParser(
                x.GetRequiredService<ILogger<HtmlParser>>(),
                x.GetRequiredService<ProxiesHandler>(),
                options
            ));
    }
}