namespace BeerApi.Application.DTOs;

public record AuditLogDto(
    int Id,
    string EntityName,
    string EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    DateTime Timestamp,
    string? UserId,
    string? UserEmail);
