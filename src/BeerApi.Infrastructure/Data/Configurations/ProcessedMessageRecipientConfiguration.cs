using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class ProcessedMessageRecipientConfiguration : IEntityTypeConfiguration<ProcessedMessageRecipient>
{
    public void Configure(EntityTypeBuilder<ProcessedMessageRecipient> builder)
    {
        builder.HasKey(recipient => new { recipient.MessageId, recipient.Consumer, recipient.RecipientHash });
        builder.Property(recipient => recipient.Consumer).HasMaxLength(100);
        builder.Property(recipient => recipient.RecipientHash).HasMaxLength(64);
        builder.HasOne<ProcessedMessage>()
            .WithMany()
            .HasForeignKey(recipient => new { recipient.MessageId, recipient.Consumer })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
