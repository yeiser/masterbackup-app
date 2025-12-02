namespace MasterBackup_API.Domain.Models;

/// <summary>
/// Information about database connection to be sent to Worker
/// </summary>
public class DatabaseConnectionInfo
{
    public Guid DatabaseConnectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty; // PostgreSQL, MySQL, SQLServer, etc.
}
