using Grimoire.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Tests;

public static class Globals {
    public static IServiceProvider Services
        => new ServiceCollection()
            .AddGrimoireSources()
            .AddLogging()
            .AddHttpClient()
            .BuildServiceProvider();
}