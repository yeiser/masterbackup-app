namespace MasterBackup_API.Application.Common.DTOs;

public class TestConnectionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public string? ServerVersion { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}
