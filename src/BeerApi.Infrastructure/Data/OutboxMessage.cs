namespace BeerApi.Infrastructure.Data;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}