using Grimoire.Sources.Sources;
using Grimoire.Web.Cache;
using Microsoft.Extensions.FileProviders;

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
    //.AddGrimoireProviders()
    .AddSingleton<TCBScansSource>()
    .AddSingleton<CacheHandler>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) {
    app.UseHsts();
}

app.UseHttpsRedirection();

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