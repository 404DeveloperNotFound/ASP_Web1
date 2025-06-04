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
        try
        {
            // Call POST api/admin/init-redis once on startup
            using (var scope = _services.CreateScope())
            {
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("http://localhost:5017");

                var response = await client.PostAsync("/api/admin/init-redis", null, stoppingToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully called init-redis API on startup.");
                }
                else
                {
                    _logger.LogWarning($"Init-redis API call failed with status code {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling init-redis API on startup");
        }

        // Now enter the regular sync loop
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