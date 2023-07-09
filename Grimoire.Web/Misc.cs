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
}