using Grimoire.Providers;
using Grimoire.Web;
using Grimoire.Web.Cache;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("settings.json");

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services
    .AddSingleton<CacheHandler>()
    .AddSingleton(new CacheOptions {
        SaveTo = Path.Combine(builder.Environment.ContentRootPath, "static")
    })
    .AddSingleton<AppState>()
    .AddGrimoireProviders()
    .AddResponseCaching()
    .AddHttpClient()
    .AddLogging(x => {
        x.ClearProviders();
        x.AddConsole();
    });

var app = builder.Build();
if (!app.Environment.IsDevelopment()) {
    app.UseHsts();
}

app.UseHttpsRedirection();

var provider = new PhysicalFileProvider(app.Services.GetRequiredService<CacheOptions>().SaveTo);
app.Environment.WebRootFileProvider = new CompositeFileProvider(
    new PhysicalFileProvider(builder.Environment.WebRootPath),
    provider
);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions {
    FileProvider = provider,
    RequestPath = "/static"
});
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();