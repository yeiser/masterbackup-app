using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Infrastructure.Services;

public class LogCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LogCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run once per day
    private readonly int _retentionDays = 5;

    public LogCleanupService(
        IServiceProvider serviceProvider,
        ILogger<LogCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Log Cleanup Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldLogsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up old logs");
            }

            // Wait for the next cleanup cycle
            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Log Cleanup Service is stopping");
    }

    private async Task CleanupOldLogsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

        try
        {
            var deletedCount = await dbContext.Logs
                .Where(log => log.TimeStamp < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation(
                    "Deleted {DeletedCount} log entries older than {CutoffDate}",
                    deletedCount,
                    cutoffDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old logs from database");
        }
    }
}
