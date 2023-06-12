using AngleSharp;
using AngleSharp.Dom;
using Grimoire.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Providers;

public static class Extensions {
    private static readonly IBrowsingContext Context
        = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

    private static readonly IReadOnlyList<string> UserAgents = new[] {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/114.0",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/78.0.3904.108 Safari/537.36 RuxitSynthetic/1.0 v3283331297382284845 t6205049005192687891"
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<IDocument> ParseAsync(this HttpClient httpClient, string url) {
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgents[Random.Shared.Next(UserAgents.Count - 1)]);

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