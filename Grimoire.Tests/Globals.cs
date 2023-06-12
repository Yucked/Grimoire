using Grimoire.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Tests;

public static class Globals {
    public static IServiceProvider Services
        => new ServiceCollection()
            .AddGrimoireProviders()
            .AddLogging()
            .AddHttpClient()
            .BuildServiceProvider();
}