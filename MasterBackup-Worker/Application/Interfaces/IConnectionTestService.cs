using MasterBackup_Worker.Domain.Entities;

namespace MasterBackup_Worker.Application.Interfaces;

/// <summary>
/// Interface for testing database connections
/// </summary>
public interface IConnectionTestService
{
    /// <summary>
    /// Tests a database connection
    /// </summary>
    /// <param name="message">Connection details</param>
    /// <returns>True if connection is successful, false otherwise</returns>
    Task<(bool Success, string Message)> TestConnectionAsync(TestConnectionMessage message);
}
