using System.Collections;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Grimoire.Web;

public static class Misc {
    public static string GetCover(this IConfiguration configuration, string localPath, string url) {
        return configuration.GetValue<bool>("Save:MangaCover")
            ? localPath
            : url;
    }

    public static bool IsNullOrEmpty(this IList list) {
        return list == null || list.Count == 0;
    }

    public static bool IsNullOrEmpty<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary) {
        return dictionary == null || dictionary.Count == 0;
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