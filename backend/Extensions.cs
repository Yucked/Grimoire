using System.Reflection;
using System.Text;
using AngleSharp.Dom;
using Grimoire.Integrations;
using Grimoire.Objects;
using Grimoire.Sources;

namespace Grimoire;

public static class Extensions {
    public static ResponseObject AsResponse(this object @object, int statusCode)
        => ResponseObject.New(statusCode, @object);

    public static ValueTask<ResponseObject> AsResponseAsync(this object @object, int statusCode)
        => ValueTask.FromResult(ResponseObject.New(statusCode, @object));

    public static string Clean(this string str)
        => string.Join(' ', str.Split(' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    public static string[] Slice(this string str, char[] separators)
        => str.Split(separators,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string[] Slice(this string str, char separator)
        => str.Split(separator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string Join(this IEnumerable<string> items)
        => string.Join(' ', items);

    public static Task<T[]> AwaitAsync<T>(this IEnumerable<Task<T>> tasks)
        => Task.WhenAll(tasks);

    public static T As<T>(this IElement element) {
        return (T)element; ;
    }

    public static string GetIdFromName(this string name)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(name));

    public static string GetNameFromId(this string id)
        => Encoding.UTF8.GetString(Convert.FromBase64String(id));

    public static MangaObject WithMetadata(this MangaObject manga, MetadataResult enrichment)
        => manga with {
            Authors    = manga.Authors.Count > 0 ? manga.Authors : enrichment.Authors,
            Artists    = enrichment.Artists,
            Genres     = manga.Genres.Count > 0  ? manga.Genres  : enrichment.Genres,
            Aliases    = [..manga.Aliases, ..enrichment.Aliases.Where(a => !manga.Aliases.Contains(a))],
            Status     = enrichment.Status,
            ReleasedOn = manga.ReleasedOn == default ? enrichment.ReleasedOn : manga.ReleasedOn,
            Ratings    = enrichment.Ratings
        };

    public static IServiceCollection AddGrimoireSources(this IServiceCollection services) {
        var sourceTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsAssignableTo(typeof(IGrimoireSource)));

        foreach (var type in sourceTypes) {
            services.AddSingleton(type);
            services.AddSingleton(typeof(IGrimoireSource), sp => sp.GetRequiredService(type));
        }

        return services;
    }
}
