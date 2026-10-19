using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.Alerts;
using OxBox.Domain.CostCentres;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class AlertRecipientConfiguration : IEntityTypeConfiguration<AlertRecipient>
{
    public void Configure(EntityTypeBuilder<AlertRecipient> builder)
    {
        builder.Property(recipient => recipient.Name).HasMaxLength(100);
        builder.Property(recipient => recipient.Email).HasMaxLength(256);
        builder.Property(recipient => recipient.Phone).HasMaxLength(32);
        builder.HasOne<CostCentre>().WithMany().HasForeignKey(recipient => recipient.CostCentreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasRowVersion();
    }
}
