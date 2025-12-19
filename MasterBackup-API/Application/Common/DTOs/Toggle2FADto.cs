using System.ComponentModel.DataAnnotations;

namespace MasterBackup_API.Application.Common.DTOs;

public class Toggle2FADto
{
    [Required]
    public bool Enable { get; set; }
}
