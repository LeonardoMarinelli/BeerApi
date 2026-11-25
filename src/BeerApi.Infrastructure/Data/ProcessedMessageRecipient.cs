namespace BeerApi.Infrastructure.Data;

public class ProcessedMessageRecipient
{
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = string.Empty;
    public string RecipientHash { get; set; } = string.Empty;
}
