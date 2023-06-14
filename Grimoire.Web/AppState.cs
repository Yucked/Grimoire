using Grimoire.Providers.Models;

namespace Grimoire.Web;

public class AppState {
    public IReadOnlyList<Manga> Mangas { get; set; }
    public Manga Manga { get; set; }
    public MangaChapter Chapter { get; set; }
}