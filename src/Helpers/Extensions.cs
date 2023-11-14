using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Grimoire.Sources.Interfaces;
using Microsoft.Extensions.FileProviders;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Grimoire.Helpers;

public static partial class Extensions {
    [GeneratedRegex("""\r\n?|\n|\s{2,}""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex Reg();

    private static readonly Regex CleanRegex
        = Reg();

    public static string GetIdFromName(this string name) {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(name));
    }

    public static string GetNameFromId(this string id) {
        return Encoding.UTF8.GetString(Convert.FromBase64String(id));
    }

    public static string Clean(this string str) {
        return
            string.IsNullOrWhiteSpace(str)
                ? str
                : CleanRegex.Replace(str, string.Empty);
    }

    public static string CleanPath(this string str) {
        return WebUtility.UrlDecode(str)
            .Replace(' ', '_');
    }

    public static string[] Slice(this string str, char seperator) {
        return string.IsNullOrWhiteSpace(str)
            ? new[] { str }
            : str.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string[] Slice(this string str, char[] seperator) {
        return string.IsNullOrWhiteSpace(str)
            ? new[] { str }
            : str.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string Join(this IEnumerable<string> strs) {
        return string.Join(" ", strs);
    }

    public static string[]? Split(this INode node, char slice) {
        return string.IsNullOrWhiteSpace(node.TextContent)
            ? default
            : node.TextContent.Split(slice);
    }

    public static Task<T[]> AwaitAsync<T>(this IEnumerable<Task<T>> results) {
        return Task.WhenAll(results);
    }

    public static T As<T>(this object element) {
        return (T)element;
    }

    public static IDictionary<string, string> ToDictionary(this Exception exception) {
        return exception
            .GetType()
            .GetProperties()
            .Select(x => new {
                x.Name,
                Value = $"{x.GetValue(exception, null)}"
            })
            .ToDictionary(k => k.Name, v => v.Value);
    }

    public static string GetRandomUserAgent(this IConfiguration configuration) {
        var userAgents = configuration.GetSection("Http:UserAgents").Get<string[]>();
        return userAgents != null
            ? userAgents[Random.Shared.Next(userAgents.Length - 1)]
            : "Mozilla/5.0 (X11; Linux x86_64) Chrome/91.0.4472.164 Safari/537.36 RuxitSynthetic/1.0";
    }

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
        foreach (var type in typeof(Extensions)
                     .Assembly
                     .GetTypes()
                     .Where(x => typeof(IGrimoireSource).IsAssignableFrom(x) && !x.IsInterface)) {
            collection.AddSingleton(type);
        }

        return collection;
    }

    public static IEnumerable<IGrimoireSource> GetGrimoireSources(this IServiceProvider provider) {
        return typeof(Extensions)
            .Assembly
            .GetTypes()
            .Where(x => typeof(IGrimoireSource).IsAssignableFrom(x) && !x.IsInterface)
            .Select(x => provider.GetRequiredService(x.UnderlyingSystemType) as IGrimoireSource);
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