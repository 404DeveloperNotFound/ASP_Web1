public class RedisSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RedisSyncBackgroundService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5);

    public RedisSyncBackgroundService(
        IServiceProvider services,
        ILogger<RedisSyncBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _services.CreateScope())
                {
                    var synchronizer = scope.ServiceProvider.GetRequiredService<RedisToDbSynchronizer>();
                    await synchronizer.SyncAllDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Redis to DB synchronization");
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }
    }
}