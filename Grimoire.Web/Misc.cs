using Grimoire.Commons.Interfaces;
using Grimoire.Sources.Sources;
using Microsoft.Extensions.FileProviders;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Grimoire.Web;

public static class Misc {
    public static string GetCover(this IConfiguration configuration, string localPath, string url) {
        return configuration.GetValue<bool>("Save:MangaCover") && !string.IsNullOrWhiteSpace(localPath)
            ? localPath
            : url;
    }

    public static async Task<bool> DoesCollectionExistAsync(this IMongoDatabase database,
                                                            string collectionName) {
        var filter = new BsonDocument("name", collectionName);
        var collections = await database.ListCollectionsAsync(
            new ListCollectionsOptions {
                Filter = filter
            });
        return await collections.AnyAsync();
    }

    public static IServiceCollection AddGrimoireSources(this IServiceCollection collection) {
        return collection
            .AddSingleton<IGrimoireSource, ArenaScansSource>()
            .AddSingleton<IGrimoireSource, AsuraScansSource>()
            .AddSingleton<IGrimoireSource, FlameScansSource>()
            .AddSingleton<IGrimoireSource, Manhwa18NetSource>()
            .AddSingleton<IGrimoireSource, PornwaClubSource>()
            .AddSingleton<IGrimoireSource, RavenScansSource>()
            //.AddSingleton<IGrimoireSource, ReaperScansSource>()
            .AddSingleton<IGrimoireSource, TCBScansSource>();
    }

    public static IEnumerable<IGrimoireSource> GetGrimoireSources(this IServiceProvider provider) {
        return provider.GetServices<IGrimoireSource>();
    }

    public static IApplicationBuilder UseCustomStaticFiles(this IApplicationBuilder builder, WebApplication app) {
        if (!Directory.Exists(app.Configuration["Save:To"])) {
            Directory.CreateDirectory(app.Configuration["Save:To"]!);
        }

        var provider = new PhysicalFileProvider(
            Path.GetFullPath(app.Configuration["Save:To"]!)
        );

        app.Environment.WebRootFileProvider = new CompositeFileProvider(
            new PhysicalFileProvider(app.Environment.WebRootPath),
            provider
        );

        app.UseStaticFiles(new StaticFileOptions {
            FileProvider = provider,
            RequestPath = $"/{app.Configuration["Save:To"]!}"
        });

        return builder;
    }
}