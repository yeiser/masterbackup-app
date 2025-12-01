using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Common.Interfaces;

public interface IDatabaseConnectionTester
{
    Task<TestConnectionResultDto> TestConnectionAsync(
        DatabaseType type,
        string host,
        int port,
        string database,
        string username,
        string password,
        string? sslMode = null,
        CancellationToken cancellationToken = default);
}
