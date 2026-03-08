namespace Grimoire;

public sealed class ServiceCoordinator {
    private readonly SemaphoreSlim _serviceReady = new(0, 1);

    public async Task WaitForServiceAsync(CancellationToken cancellationToken) {
        await _serviceReady.WaitAsync(cancellationToken);
    }

    public void ServiceIsReady() {
        _serviceReady.Release();
    }
}