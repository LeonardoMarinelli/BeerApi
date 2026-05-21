using BeerApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeerApi.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.EntityId).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(20);
        builder.Property(a => a.UserId).HasMaxLength(450);
        builder.Property(a => a.UserEmail).HasMaxLength(256);

        builder.Property(a => a.OldValues).HasColumnType("text");
        builder.Property(a => a.NewValues).HasColumnType("text");

        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.EntityName);
        builder.HasIndex(a => a.UserId);
    }
}
