using AngleSharp;
using AngleSharp.Dom;
using Grimoire.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Providers;

public static class Extensions {
    private static readonly IBrowsingContext Context
        = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<IDocument> ParseAsync(this HttpClient httpClient, string url) {
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