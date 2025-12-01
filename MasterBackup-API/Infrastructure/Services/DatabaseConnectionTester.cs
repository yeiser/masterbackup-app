using System.Diagnostics;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Enums;
using Npgsql;

namespace MasterBackup_API.Infrastructure.Services;

public class DatabaseConnectionTester : IDatabaseConnectionTester
{
    private readonly ILogger<DatabaseConnectionTester> _logger;

    public DatabaseConnectionTester(ILogger<DatabaseConnectionTester> logger)
    {
        _logger = logger;
    }

    public async Task<TestConnectionResultDto> TestConnectionAsync(
        DatabaseType type,
        string host,
        int port,
        string database,
        string username,
        string password,
        string? sslMode = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = type switch
            {
                DatabaseType.PostgreSQL => await TestPostgreSQLAsync(host, port, database, username, password, sslMode, cancellationToken),
                DatabaseType.MySQL => throw new NotImplementedException("MySQL support coming soon"),
                DatabaseType.SQLServer => throw new NotImplementedException("SQL Server support coming soon"),
                DatabaseType.MongoDB => throw new NotImplementedException("MongoDB support coming soon"),
                DatabaseType.MariaDB => throw new NotImplementedException("MariaDB support coming soon"),
                _ => new TestConnectionResultDto
                {
                    Success = false,
                    Message = "Database type not supported",
                    ResponseTime = stopwatch.Elapsed
                }
            };

            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.TestedAt = DateTime.UtcNow;
            return result;
        }
        catch (NotImplementedException ex)
        {
            stopwatch.Stop();
            return new TestConnectionResultDto
            {
                Success = false,
                Message = ex.Message,
                ResponseTime = stopwatch.Elapsed,
                TestedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error testing database connection to {Host}:{Port}", host, port);
            
            return new TestConnectionResultDto
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}",
                ResponseTime = stopwatch.Elapsed,
                TestedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<TestConnectionResultDto> TestPostgreSQLAsync(
        string host, 
        int port, 
        string database, 
        string username, 
        string password,
        string? sslMode,
        CancellationToken cancellationToken)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            Timeout = 10,
            CommandTimeout = 10
        };

        // Configurar SSL Mode si se proporciona
        if (!string.IsNullOrEmpty(sslMode))
        {
            connectionStringBuilder.SslMode = sslMode.ToLower() switch
            {
                "disable" => SslMode.Disable,
                "allow" => SslMode.Allow,
                "prefer" => SslMode.Prefer,
                "require" => SslMode.Require,
                "verify-ca" => SslMode.VerifyCA,
                "verify-full" => SslMode.VerifyFull,
                _ => SslMode.Prefer
            };
        }

        await using var connection = new NpgsqlConnection(connectionStringBuilder.ToString());
        
        try
        {
            await connection.OpenAsync(cancellationToken);
            
            // Obtener versión del servidor y información adicional
            await using var command = new NpgsqlCommand(@"
                SELECT 
                    version() as version,
                    current_database() as database,
                    inet_server_addr() as server_addr,
                    inet_server_port() as server_port
            ", connection);
            
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            string? serverVersion = null;
            
            if (await reader.ReadAsync(cancellationToken))
            {
                serverVersion = reader["version"]?.ToString();
            }

            return new TestConnectionResultDto
            {
                Success = true,
                Message = "Connection successful",
                ServerVersion = serverVersion?.Split('\n')[0].Trim()
            };
        }
        catch (PostgresException pgEx)
        {
            var errorMessage = pgEx.SqlState switch
            {
                "28P01" => "Authentication failed: Invalid username or password",
                "3D000" => $"Database '{database}' does not exist",
                "28000" => "Invalid authorization specification",
                "08006" => "Connection failure: Unable to connect to server",
                _ => $"PostgreSQL error: {pgEx.Message}"
            };

            return new TestConnectionResultDto
            {
                Success = false,
                Message = errorMessage
            };
        }
        catch (NpgsqlException npgEx)
        {
            var errorMessage = npgEx.InnerException?.Message ?? npgEx.Message;
            
            if (errorMessage.Contains("timeout"))
            {
                errorMessage = "Connection timeout: Server is unreachable or too slow to respond";
            }
            else if (errorMessage.Contains("host"))
            {
                errorMessage = $"Unable to resolve host '{host}' or server is not running";
            }

            return new TestConnectionResultDto
            {
                Success = false,
                Message = errorMessage
            };
        }
    }
}
