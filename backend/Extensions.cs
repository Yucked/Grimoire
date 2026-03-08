using System.Reflection;
using System.Text;
using AngleSharp.Dom;
using Grimoire.Integrations;
using Grimoire.Objects;
using Grimoire.Sources;

namespace Grimoire;

public static class Extensions {
    extension(object @object) {
        public ResponseObject AsResponse(int statusCode) {
            return ResponseObject.New(statusCode, @object);
        }

        public ValueTask<ResponseObject> AsResponseAsync(int statusCode) {
            return ValueTask.FromResult(ResponseObject.New(statusCode, @object));
        }
    }

    extension(string str) {
        public string Clean() {
            return string.Join(' ', str.Split(' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        public string[] Slice(char[] separators) {
            return str.Split(separators,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public string[] Slice(char separator) {
            return str.Split(separator,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    public static string Join(this IEnumerable<string> items) {
        return string.Join(' ', items);
    }

    public static Task<T[]> AwaitAsync<T>(this IEnumerable<Task<T>> tasks) {
        return Task.WhenAll(tasks);
    }

    public static T As<T>(this IElement element) {
        return (T)element;
    }

    extension(string name) {
        public string GetIdFromName() {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(name)).ToLowerInvariant();
        }

        public string GetNameFromId() {
            return Encoding.UTF8.GetString(Convert.FromBase64String(name)).ToLowerInvariant();
        }
    }

    public static MangaObject WithMetadata(this MangaObject manga, MetadataResult enrichment) {
        return manga with {
            Authors = manga.Authors.Count > 0 ? manga.Authors : enrichment.Authors,
            Artists = enrichment.Artists,
            Genres = manga.Genres.Count > 0 ? manga.Genres : enrichment.Genres,
            Aliases = [..manga.Aliases, ..enrichment.Aliases.Where(a => !manga.Aliases.Contains(a))],
            Status = enrichment.Status,
            ReleasedOn = manga.ReleasedOn == default ? enrichment.ReleasedOn : manga.ReleasedOn,
            Ratings = enrichment.Ratings
        };
    }

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