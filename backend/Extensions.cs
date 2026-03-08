using System.Reflection;
using System.Text;
using AngleSharp.Dom;
using FlareSolverrSharp;
using Grimoire.Objects;
using Grimoire.Sources.Commons;
using Grimoire.Sources.MetadataProviders;

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
            return Convert.ToHexStringLower(Encoding.UTF8.GetBytes(name));
        }

        public string GetNameFromId() {
            return Encoding.UTF8.GetString(Convert.FromHexString(name));
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

    extension(IServiceCollection services) {
        public IServiceCollection AddGrimoireSources() {
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

        public IServiceCollection AddHttpClient<T>(IConfigurationManager configuration,
                                                   Action<HttpClient>? configureClient = null)
            where T : class {
            var x = services.AddHttpClient<T>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new ClearanceHandler(configuration.GetValue<string>("Http:FlareUrl")) {
                        MaxTimeout = configuration.GetValue<int>("Http:FlareTimeout")
                    })
                .RemoveAllLoggers();

            if (configureClient != null) {
                x.ConfigureHttpClient(configureClient);
            }

            return services;
        }
    }
}