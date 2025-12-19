namespace MasterBackup_API.Application.Common.DTOs;

public class BackupStatsDto
{
    public int Pending { get; set; }
    public int InProgress { get; set; }
    public int Failed { get; set; }
    public int Completed { get; set; }
    public int Total { get; set; }
}
