namespace BeerApi.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    /// <summary>Name of the entity that was modified (e.g. "Beer", "Brewery").</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>String representation of the primary key of the modified entity.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>The operation performed: Create, Update or Delete.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>JSON snapshot of old values (null for Create).</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of new values (null for Delete).</summary>
    public string? NewValues { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Identity user ID of the person who triggered the change. Null for system/seed operations.</summary>
    public string? UserId { get; set; }

    /// <summary>Email of the person who triggered the change. Null for system/seed operations.</summary>
    public string? UserEmail { get; set; }
}
