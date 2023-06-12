using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Grimoire.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Providers;

public static partial class Extensions {
    private static readonly IBrowsingContext Context
        = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

    private static readonly IReadOnlyList<string> UserAgents = new[] {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/114.0",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/78.0.3904.108 Safari/537.36 RuxitSynthetic/1.0 v3283331297382284845 t6205049005192687891",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 10_1_4; like Mac OS X) AppleWebKit/601.14 (KHTML, like Gecko)  Chrome/51.0.1615.309 Mobile Safari/535.3",
        "Mozilla / 5.0 (compatible; MSIE 10.0; Windows; U; Windows NT 10.4; WOW64 Trident / 6.0)",
        "Mozilla/5.0 (Linux; U; Linux x86_64; en-US) AppleWebKit/534.29 (KHTML, like Gecko) Chrome/49.0.3483.101 Safari/534",
        "Mozilla/5.0 (Windows; U; Windows NT 10.1;; en-US) AppleWebKit/535.14 (KHTML, like Gecko) Chrome/51.0.2258.396 Safari/600",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_6_5; en-US) AppleWebKit/600.21 (KHTML, like Gecko) Chrome/48.0.1544.246 Safari/536"
    };

    [GeneratedRegex("\\r\\n?|\\n")]
    private static partial Regex CleanRegex();

    public static string Clean(this string str) {
        return CleanRegex().Replace(str, string.Empty);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<IDocument> ParseAsync(this HttpClient httpClient, string url) {
        //TODO: Probably should seperate that based on Provider
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgents[Random.Shared.Next(UserAgents.Count - 1)]);
        httpClient.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");

        var responseMessage = await httpClient.GetAsync(url);
        if (!responseMessage.IsSuccessStatusCode) {
            throw new Exception(responseMessage.ReasonPhrase);
        }

        await using var stream = await responseMessage.Content.ReadAsStreamAsync();
        var document = await Context.OpenAsync(x => x.Content(stream));
        return document;
    }

    public static IServiceCollection AddGrimoireProviders(this IServiceCollection collection) {
        var providers = typeof(IGrimoireProvider).Assembly
            .GetTypes()
            .Where(x => typeof(IGrimoireProvider).IsAssignableFrom(x)
                        && !x.IsInterface
                        && !x.IsAbstract);

        foreach (var provider in providers) {
            collection.AddSingleton(provider);
        }

        return collection;
    }

    public static IEnumerable<IGrimoireProvider> GetProviders(this IServiceProvider provider) {
        var providers = typeof(IGrimoireProvider).Assembly
            .GetTypes()
            .Where(x => typeof(IGrimoireProvider).IsAssignableFrom(x)
                        && !x.IsInterface
                        && !x.IsAbstract);

        return providers
            .Select(provider.GetRequiredService)
            .OfType<IGrimoireProvider>();
    }
}