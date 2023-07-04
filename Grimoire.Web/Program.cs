using Grimoire.Sources.Miscellaneous;
using Grimoire.Web.Handlers;
using Microsoft.Extensions.FileProviders;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("config.json", false, true);

builder
    .Services
    .AddRazorPages()
    .Services
    .AddServerSideBlazor()
    .Services
    .AddResponseCaching()
    .AddHttpClient()
    .AddMemoryCache()
    .AddLogging(x => {
        x.ClearProviders();
        x.AddConsole();
    })
    .AddGrimoireSources()
    .AddSingleton(new
            MongoClient(builder.Configuration["Mongo"])
        .GetDatabase(nameof(Grimoire)))
    .AddSingleton<CacheHandler>()
    .AddSingleton<DbHandler>()
    .AddSingleton<LibraryHandler>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) {
    app.UseHsts();
}

app.UseHttpsRedirection();

if (!Directory.Exists(app.Configuration["Save:To"])) {
    Directory.CreateDirectory(app.Configuration["Save:To"]!);
}

var provider = new PhysicalFileProvider(
    Path.GetFullPath(app.Configuration["Save:To"]!)
);

app.Environment.WebRootFileProvider = new CompositeFileProvider(
    new PhysicalFileProvider(builder.Environment.WebRootPath),
    provider
);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions {
    FileProvider = provider,
    RequestPath = $"/{app.Configuration["Save:To"]!}"
});

app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();