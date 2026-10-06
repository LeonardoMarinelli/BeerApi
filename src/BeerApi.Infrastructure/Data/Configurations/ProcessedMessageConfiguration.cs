using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.HasKey(message => new { message.MessageId, message.Consumer });
        builder.Property(message => message.Consumer).HasMaxLength(100);
        builder.Property(message => message.LastError).HasMaxLength(2000);
    }
}