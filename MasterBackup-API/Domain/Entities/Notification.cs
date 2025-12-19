using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterBackup_API.Domain.Entities;

/// <summary>
/// Notificación para usuarios del sistema
/// </summary>
[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [Column("user_id")]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column("type")]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [Column("title")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Column("redirect_url")]
    [MaxLength(500)]
    public string? RedirectUrl { get; set; }

    [Column("related_entity_id")]
    public Guid? RelatedEntityId { get; set; }

    [Column("related_entity_type")]
    [MaxLength(100)]
    public string? RelatedEntityType { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    // Note: No navigation property to ApplicationUser because users are in MasterDb, not TenantDb
    // UserId is a string reference without FK constraint
}
