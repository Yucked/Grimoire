using Grimoire.Handlers;

namespace Grimoire.Services;

public sealed class LibraryBackgroundService(
    ILogger<LibraryBackgroundService> logger,
    DatabaseHandler databaseHandler,
    ServiceCoordinator serviceCoordinator) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await serviceCoordinator.WaitForServiceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested) {
            await RefreshLibraryAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromDays(5), stoppingToken);
        }
    }

    public async Task RefreshLibraryAsync(CancellationToken cancellationToken = default) {
        var users = await databaseHandler.GetUsersAsync();
        try {
            foreach (var user in users) {
                await databaseHandler.TryRefreshLibraryAsync(user.Id, cancellationToken);
            }
        }
        catch (Exception ex) {
            logger.LogError("{exMessage}", ex.Message);
        }
    }
}