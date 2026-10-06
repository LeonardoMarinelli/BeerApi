namespace BeerApi.Infrastructure.Data;

public class ProcessedMessage
{
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? DeadLetteredAt { get; set; }
    public string? LastError { get; set; }
}