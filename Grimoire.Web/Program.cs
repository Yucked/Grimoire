using Grimoire.Providers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services
    .AddHttpClient()
    .AddLogging(x => {
        x.ClearProviders();
        x.AddConsole();
    })
    .AddSingleton<Manhwa18Provider>()
    .AddSingleton<TCBScansProvider>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) {
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();