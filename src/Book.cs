using Grimoire.Models;

namespace Grimoire;

internal record Page {
    public int Index { get; set; }
    public IList<Manga> Items { get; set; }
}

internal readonly record struct Book {
    public IReadOnlyList<Page> Pages { get; }
    public bool HasAnyPages { get; }
    public int ItemCount { get; }

    public Book(IReadOnlyCollection<Manga>? items) {
        ItemCount = items.Count;

        Pages = items
            .OrderBy(x => x.Name)
            .Chunk(16)
            .Select((x, y) => new Page {
                Index = y,
                Items = x
            })
            .ToArray();

        HasAnyPages = Pages.Count != 0;
    }

    public Page GoTo(int pageNumber) {
        return Pages[pageNumber];
    }

    public bool IsValidPage(int number) {
        return Enumerable.Range(0, Pages.Count)
            .Contains(number);
    }
}