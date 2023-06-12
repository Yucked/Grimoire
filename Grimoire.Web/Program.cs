using Grimoire.Providers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services
    .AddGrimoireProviders()
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
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();