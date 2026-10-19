using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.Alerts;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.Ignore(message => message.IsSent);
        builder.Property(message => message.Recipient).HasMaxLength(256);
        builder.Property(message => message.Subject).HasMaxLength(300);
        builder.Property(message => message.LastError).HasMaxLength(500);

        builder.HasOne(message => message.Alert).WithMany().HasForeignKey(message => message.AlertId).OnDelete(DeleteBehavior.Restrict);

        // The dispatcher looks only at unsent messages that are due.
        builder.HasIndex(message => message.NextAttemptAtUtc).HasFilter("\"SentAtUtc\" IS NULL");
    }
}
