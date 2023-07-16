using Grimoire.Commons.Interfaces;
using Grimoire.Sources.Sources;
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
}