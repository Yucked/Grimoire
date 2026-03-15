using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using FlareSolverrSharp;
using Grimoire.Handlers;
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

        public string GetIdFromName() {
            return Convert.ToHexStringLower(Encoding.UTF8.GetBytes(str));
        }

        public string GetNameFromId() {
            return Encoding.UTF8.GetString(Convert.FromHexString(str));
        }

        public double Similarity(string b) {
            var tokensA = Tokenize(str);
            var tokensB = Tokenize(b);

            var intersection = tokensA.Intersect(tokensB).Count();
            var union = tokensA.Union(tokensB).Count();

            var jaccard = (double)intersection / union;
            var containment = (double)intersection / Math.Min(tokensA.Count, tokensB.Count);

            return Math.Max(jaccard, containment * 0.9) * 100.0;

            static List<string> Tokenize(string s) =>
                Regex.Replace(s.ToLowerInvariant(), @"[^\w\s]", " ")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
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

    extension(MangaObject manga) {
        public MangaObject WithMetadata(MetadataResult enrichment) {
            return manga with {
                Authors = manga.Authors.Any() ? manga.Authors : enrichment.Authors,
                Artists = enrichment.Artists,
                Genres = manga.Genres.Any() ? manga.Genres : enrichment.Genres,
                Aliases = [..manga.Aliases, ..enrichment.Aliases.Where(a => !manga.Aliases.Contains(a))],
                Status = enrichment.Status,
                ReleasedOn = manga.ReleasedOn == default ? enrichment.ReleasedOn : manga.ReleasedOn,
                Ratings = enrichment.Ratings
            };
        }

        public async Task<MangaObject> EnrichWithMetadataAsync(string name,
                                                               IEnumerable<IMetadataProvider> providers,
                                                               ILogger logger) {
            foreach (var provider in providers)
                try {
                    var enrichment = await provider.FindMangaAsync(name);
                    if (enrichment is null) {
                        continue;
                    }

                    manga = manga.WithMetadata(enrichment);
                    break;
                }
                catch (Exception ex) {
                    logger.LogWarning(ex, "Metadata enrichment failed for {name} via {providerName}",
                        name, provider.GetType().Name);
                }

            return manga;
        }
    }

    public static async Task<string> SaveCoverSafeAsync(
        this ScrapingHandler scrapingHandler,
        string cover,
        string sourceId,
        string name,
        ILogger logger) {
        try {
            return await scrapingHandler.SaveCoverAsync(cover, sourceId, name);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to download cover for {}", name);
            return string.Empty;
        }
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

        public IServiceCollection AddMetadataProviders() {
            var sourceTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false }
                            && t.IsAssignableTo(typeof(IMetadataProvider)));

            foreach (var type in sourceTypes) {
                services.AddSingleton(type);
                services.AddSingleton(typeof(IMetadataProvider), sp => sp.GetRequiredService(type));
            }

            return services;
        }

        public IServiceCollection AddFlareHttpClient(IConfiguration configuration) {
            var handler = new ClearanceHandler(configuration.GetValue<string>("Http:FlareUrl")) {
                ProxyUrl = configuration.GetValue<string>("Http:FlareProxyUrl")
            };

            services.AddSingleton(new HttpClient(handler) {
                DefaultRequestVersion = HttpVersion.Version11,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            });
            return services;
        }
    }

    public static void AddRange<T>(this IList<T> list, IEnumerable<T> range) {
        foreach (var item in range) {
            list.Add(item);
        }
    }
}