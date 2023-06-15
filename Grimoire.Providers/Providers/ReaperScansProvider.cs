using Grimoire.Providers.Interfaces;
using Grimoire.Providers.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Providers;

// This is a fun one
public class ReaperScansProvider : IGrimoireProvider {
    public string Name
        => "Reaper Scans";

    public string BaseUrl
        => "https://reaperscans.com";

    public string Icon
        => $"{BaseUrl}/images/icons/310x310.png";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ReaperScansProvider> _logger;

    public async Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        using var responseMessage = await _httpClient.GetAsync($"{BaseUrl}/comics?page=5");
        if (!responseMessage.IsSuccessStatusCode) {
            _logger.LogError("Couldn't get any response because: {response}", responseMessage.ReasonPhrase);
            throw new HttpRequestException(responseMessage.ReasonPhrase);
        }

        await responseMessage.Content.ReadAsByteArrayAsync();

        return default;
    }

    public async Task<IReadOnlyList<Manga>> PaginateAsync(int page) {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<MangaChapter>> FetchChaptersAsync(Manga manga) {
        throw new NotImplementedException();
    }
}