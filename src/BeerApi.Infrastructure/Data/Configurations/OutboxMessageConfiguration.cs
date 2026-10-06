using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(message => message.Id);
        builder.Property(message => message.EventType).HasMaxLength(100).IsRequired();
        builder.Property(message => message.Payload).HasColumnType("json").IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(2000);
        builder.HasIndex(message => new { message.ProcessedAt, message.NextAttemptAt, message.OccurredAt });
    }
}