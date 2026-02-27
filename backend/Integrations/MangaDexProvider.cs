using System.Text.Json;
using Grimoire.Objects;

namespace Grimoire.Integrations;

public sealed class MangaDexProvider(
    HttpClient httpClient,
    ILogger<MangaDexProvider> logger) : IMetadataProvider {

    private static readonly JsonSerializerOptions JSON_OPTIONS
        = new() { PropertyNameCaseInsensitive = true };

    public async Task<MetadataResult?> FindMangaAsync(string title) {
        try {
            var searchUrl = $"/manga?title={Uri.EscapeDataString(title)}&limit=5&includes[]=author&includes[]=artist";
            using var searchResponse = await httpClient.GetAsync(searchUrl);
            searchResponse.EnsureSuccessStatusCode();

            await using var searchStream = await searchResponse.Content.ReadAsStreamAsync();
            using var searchDoc = await JsonDocument.ParseAsync(searchStream);

            var data = searchDoc.RootElement.GetProperty("data");
            string? matchedId = null;
            JsonElement matchedEntry = default;

            foreach (var entry in data.EnumerateArray()) {
                var attrs = entry.GetProperty("attributes");
                if (TitleMatches(attrs, title)) {
                    matchedId = entry.GetProperty("id").GetString();
                    matchedEntry = entry;
                    break;
                }
            }

            if (matchedId is null) return null;

            var attrs2 = matchedEntry.GetProperty("attributes");
            var relationships = matchedEntry.GetProperty("relationships");

            var authors = ExtractRelationshipNames(relationships, "author");
            var artists = ExtractRelationshipNames(relationships, "artist");
            var genres = ExtractGenres(attrs2);
            var aliases = ExtractAliases(attrs2);
            var status = ParseStatus(attrs2.GetProperty("status").GetString());
            var year = attrs2.TryGetProperty("year", out var yearEl) && yearEl.ValueKind != JsonValueKind.Null
                ? new DateOnly(yearEl.GetInt32(), 1, 1)
                : default;

            var ratings = await FetchRatingsAsync(matchedId);

            return new MetadataResult(matchedId, authors, artists, genres, aliases, status, year, ratings);
        }
        catch (Exception ex) {
            logger.LogError(ex, "MangaDex lookup failed for {Title}", title);
            return null;
        }
    }

    private async Task<float> FetchRatingsAsync(string mangaId) {
        try {
            using var response = await httpClient.GetAsync($"/statistics/manga/{mangaId}");
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var rating = doc.RootElement
                .GetProperty("statistics")
                .GetProperty(mangaId)
                .GetProperty("rating")
                .GetProperty("bayesian");
            return rating.ValueKind == JsonValueKind.Null ? 0f : (float)rating.GetDouble();
        }
        catch {
            return 0f;
        }
    }

    private static bool TitleMatches(JsonElement attrs, string search) {
        var lower = search.ToLowerInvariant();

        var titleProp = attrs.GetProperty("title");
        foreach (var kv in titleProp.EnumerateObject()) {
            if (kv.Value.GetString()?.ToLowerInvariant().Contains(lower) is true)
                return true;
        }

        if (!attrs.TryGetProperty("altTitles", out var altTitles)) return false;
        foreach (var alt in altTitles.EnumerateArray()) {
            foreach (var kv in alt.EnumerateObject()) {
                if (kv.Value.GetString()?.ToLowerInvariant().Contains(lower) is true)
                    return true;
            }
        }

        return false;
    }

    private static IList<string> ExtractRelationshipNames(JsonElement relationships, string type) {
        var result = new List<string>();
        foreach (var rel in relationships.EnumerateArray()) {
            if (rel.GetProperty("type").GetString() != type) continue;
            if (!rel.TryGetProperty("attributes", out var relAttrs)) continue;
            var name = relAttrs.GetProperty("name").GetString();
            if (name is not null) result.Add(name);
        }
        return result;
    }

    private static IList<string> ExtractGenres(JsonElement attrs) {
        var result = new List<string>();
        if (!attrs.TryGetProperty("tags", out var tags)) return result;
        foreach (var tag in tags.EnumerateArray()) {
            var tagAttrs = tag.GetProperty("attributes");
            if (tagAttrs.GetProperty("group").GetString() != "genre") continue;
            var name = tagAttrs.GetProperty("name").GetProperty("en").GetString();
            if (name is not null) result.Add(name);
        }
        return result;
    }

    private static IList<string> ExtractAliases(JsonElement attrs) {
        var result = new List<string>();
        if (!attrs.TryGetProperty("altTitles", out var altTitles)) return result;
        foreach (var alt in altTitles.EnumerateArray()) {
            foreach (var kv in alt.EnumerateObject()) {
                var val = kv.Value.GetString();
                if (val is not null) result.Add(val);
            }
        }
        return result;
    }

    private static MangaStatus ParseStatus(string? status) => status switch {
        "ongoing" => MangaStatus.OnGoing,
        "completed" => MangaStatus.Completed,
        "hiatus" => MangaStatus.Hiatus,
        "cancelled" => MangaStatus.Cancelled,
        _ => MangaStatus.OnGoing
    };
}
