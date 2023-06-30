using Grimoire.Sources;
using Grimoire.Sources.Miscellaneous;

namespace Grimoire.Web;

public record struct PathMaker {
    public static PathMaker New(string basePath) => new(basePath);

    public string Ave { get; private set; }

    private PathMaker(string basePath) {
        Ave = basePath;
    }

    public PathMaker WithSource(string sourceId) {
        Ave = Path.Combine(Ave, sourceId);
        return this;
    }

    public PathMaker WithManga(string mangaId) {
        Ave = Path.Combine(Ave, mangaId);
        return this;
    }

    public PathMaker WithChapter(int chapter) {
        Ave = Path.Combine(Ave, $"{chapter}");
        return this;
    }

    public string WithIcon(string sourceIcon) {
        return Path.Combine(Ave, sourceIcon.CleanPath().Split('/')[^1]);
    }

    public string WithCover(string sourceIcon) {
        return WithIcon(sourceIcon);
    }

    public string WithPage(string chapterPage) {
        return WithIcon(chapterPage);
    }

    public PathMaker Verify() {
        if (Directory.Exists(Ave)) {
            return this;
        }

        Directory.CreateDirectory(Ave!);
        return this;
    }

    public override string ToString() {
        return Ave;
    }
}