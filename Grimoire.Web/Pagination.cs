using Grimoire.Sources.Models;

namespace Grimoire.Web;

public record Page {
    public IReadOnlyList<Manga> Items { get; set; }
    public Page Previous { get; set; }
    public Page Next { get; set; }
}

public record struct Pagination {
    public Page Active { get; private set; }

    public bool HasPages
        => Count > 1;

    public int Count { get; }

    public Pagination(IEnumerable<Manga> mangas) {
        var index = 0;
        var pages = mangas
            .OrderBy(x => x.Name)
            .Chunk(15)
            .ToArray();

        do {
            var page = new Page {
                Items = pages[index]
            };

            if (Active == null) {
                Active = page;
                index++;
                continue;
            }

            var last = GetLastPage();
            last.Next = page;
            page.Previous = last;
            index++;
        } while (index < pages.Length);

        Count = index;
    }

    private Page GetLastPage() {
        var active = Active;
        while (active.Next != null) {
            active = active.Next;
        }

        return active;
    }

    public void Next() {
        var active = Active;
        Active = active.Next;
    }

    public void Previous() {
        var active = Active;
        Active = active.Previous;
    }
}