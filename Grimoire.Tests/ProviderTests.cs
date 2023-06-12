using Grimoire.Providers;
using Grimoire.Providers.Interfaces;
using Grimoire.Providers.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Tests;

[TestClass]
public sealed class ProviderTests {
    private readonly Type _provider
        = typeof(FlameScansProvider);
    
    [TestMethod]
    public async Task FetchMangasAsync() {
        var provider = Globals.Services.GetRequiredService(_provider) as IGrimoireProvider;
        Assert.IsNotNull(provider);

        var mangas = await provider.FetchMangasAsync();
        Assert.IsNotNull(mangas);
        Assert.IsTrue(mangas.Count > 0);
    }

    [TestMethod]
    public async Task FetchChaptersAsync() {
        var provider = Globals.Services.GetRequiredService(_provider) as IGrimoireProvider;
        Assert.IsNotNull(provider);

        var manga = new Manga {
            Url = "https://flamescans.org/series/1686564121-forty-eight-hours-a-day/"
        };
        
        var chapters = await provider.FetchChaptersAsync(manga);
        Assert.IsNotNull(chapters);
        Assert.IsTrue(chapters.Count > 0);
    }
}