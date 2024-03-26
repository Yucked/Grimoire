using Grimoire.Handlers;
using Grimoire.Objects;
using LiteDB;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("config.json", false, true);

builder.Services.AddControllers();
builder.Services
    .AddOutputCache()
    .AddMemoryCache(x => {
        x.CompactionPercentage = 0.80f;
        x.ExpirationScanFrequency = TimeSpan.FromMinutes(30);
    })
    .AddSingleton<DatabaseHandler>()
    .AddSingleton<ILiteDatabase>(x => {
        var database = new LiteDatabase("grimoire.db");
        var collection = database.GetCollection<MangaObject>();
        collection.EnsureIndex(e => e.Aliases);
        collection.EnsureIndex(e => e.Artists);
        collection.EnsureIndex(e => e.Authors);
        collection.EnsureIndex(e => e.Title);
        collection.EnsureIndex(e => e.Summary);
        collection.EnsureIndex(e => e.Genres);
        return database;
    })
    .AddHostedService<BackgroundHandler>();

var app = builder.Build();
app.MapControllers();
app.UseOutputCache(); // what does it do?
// app.UseResponseCaching();
// app.UseResponseCompression();

await app.RunAsync();