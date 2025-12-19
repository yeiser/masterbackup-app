namespace MasterBackup_API.Application.Common.DTOs;

public class ApiKeyLogDto
{
    public Guid Id { get; set; }
    public string RemoteIp { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Endpoint { get; set; }
    public string? Method { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApiKeyLogsResponseDto
{
    public List<ApiKeyLogDto> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public bool HasMore { get; set; }
}
