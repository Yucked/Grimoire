using System.Threading.Channels;

namespace Grimoire;

public record DownloadQueue {
    private readonly Channel<(string SourceId, string MangaId, string ChapterNumber, string[] ImageUrls)> _channel;

    public ChannelReader<(string SourceId, string MangaId, string ChapterNumber, string[] ImageUrls)>
        Reader => _channel.Reader;

    public DownloadQueue() {
        _channel =
            Channel.CreateUnbounded<(string SourceId, string MangaId, string ChapterNumber, string[] ImageUrls)>();
    }

    public ValueTask AddAsync(string sourceId, string mangaId, string chapterNumber, string[] imageUrls) {
        return _channel.Writer.WriteAsync((sourceId, mangaId, chapterNumber, imageUrls));
    }
}