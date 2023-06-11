using AngleSharp;
using AngleSharp.Dom;

namespace Grimoire.Common;

public static class Extensions {
    private static readonly IBrowsingContext Context
        = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<IDocument> ParseAsync(this HttpClient httpClient, string url) {
        var responseMessage = await httpClient.GetAsync(url);
        if (!responseMessage.IsSuccessStatusCode) {
            throw new Exception(responseMessage.ReasonPhrase);
        }

        await using var stream = await responseMessage.Content.ReadAsStreamAsync();
        var document = await Context.OpenAsync(x => x.Content(stream));
        return document;
    }
}