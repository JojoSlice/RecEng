using Npgsql;
using StackExchange.Redis;

namespace recommendation_service;

public class Worker(
    ILogger<Worker> logger,
    [FromKeyedServices("postgres")] NpgsqlDataSource postgres,
    [FromKeyedServices("timescale")] NpgsqlDataSource timescale,
    IConnectionMultiplexer redis
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
