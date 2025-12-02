using Microsoft.Extensions.Logging;
using MasterBackup_Worker.Application.Interfaces;
using MasterBackup_Worker.Domain.Entities;
using MasterBackup_Worker.Domain.Enums;
using Npgsql;

namespace MasterBackup_Worker.Application.Services;

/// <summary>
/// Service for testing database connections
/// </summary>
public class ConnectionTestService : IConnectionTestService
{
    private readonly ILogger<ConnectionTestService> _logger;

    public ConnectionTestService(ILogger<ConnectionTestService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(TestConnectionMessage message)
    {
        _logger.LogInformation("Testing connection to {DatabaseType} database: {Host}:{Port}/{Database}", 
            message.Type, message.Host, message.Port, message.Database);

        try
        {
            return message.Type switch
            {
                DatabaseType.PostgreSQL => await TestPostgreSQLConnectionAsync(message),
                DatabaseType.MySQL => await TestMySQLConnectionAsync(message),
                DatabaseType.SQLServer => await TestSQLServerConnectionAsync(message),
                _ => (false, $"Database type {message.Type} is not supported yet")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to {DatabaseType} database {Host}:{Port}/{Database}", 
                message.Type, message.Host, message.Port, message.Database);
            return (false, $"Connection test failed: {ex.Message}");
        }
    }

    private async Task<(bool Success, string Message)> TestPostgreSQLConnectionAsync(TestConnectionMessage message)
    {
        var connectionString = BuildPostgreSQLConnectionString(message);

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Get PostgreSQL version to verify connection
            await using var command = new NpgsqlCommand("SELECT version()", connection);
            var version = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            var versionString = version?.ToString() ?? "Unknown version";
            _logger.LogInformation("Successfully connected to PostgreSQL: {Version}", versionString);
            
            return (true, $"Connection successful! PostgreSQL version: {versionString}");
        }
        catch (NpgsqlException ex)
        {
            _logger.LogWarning(ex, "PostgreSQL connection failed: {Message}", ex.Message);
            return (false, $"PostgreSQL connection failed: {ex.Message}");
        }
    }

    private Task<(bool Success, string Message)> TestMySQLConnectionAsync(TestConnectionMessage message)
    {
        // TODO: Implement MySQL connection test
        _logger.LogWarning("MySQL connection test not implemented yet");
        return Task.FromResult((false, "MySQL support not implemented yet"));
    }

    private Task<(bool Success, string Message)> TestSQLServerConnectionAsync(TestConnectionMessage message)
    {
        // TODO: Implement SQL Server connection test
        _logger.LogWarning("SQL Server connection test not implemented yet");
        return Task.FromResult((false, "SQL Server support not implemented yet"));
    }

    private string BuildPostgreSQLConnectionString(TestConnectionMessage message)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = message.Host,
            Port = message.Port,
            Database = message.Database,
            Username = message.Username,
            Password = message.Password,
            Timeout = 15,
            CommandTimeout = 30
        };

        // Apply SSL mode if specified
        if (!string.IsNullOrEmpty(message.SSLMode))
        {
            if (Enum.TryParse<SslMode>(message.SSLMode, true, out var sslMode))
            {
                builder.SslMode = sslMode;
            }
        }

        return builder.ToString();
    }
}
