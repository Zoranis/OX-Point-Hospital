using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.Alerts;
using OxBox.Domain.CostCentres;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.Ignore(alert => alert.IsActive);
        builder.Property(alert => alert.Key).HasMaxLength(200);
        builder.Property(alert => alert.BoxCode).HasMaxLength(32);
        builder.Property(alert => alert.Details).HasMaxLength(1000);

        builder.HasOne<CostCentre>().WithMany().HasForeignKey(alert => alert.CostCentreId).OnDelete(DeleteBehavior.Restrict);

        // Each condition is active at most once; that is what keeps an alert from being sent twice.
        builder.HasIndex(alert => alert.Key)
            .IsUnique()
            .HasDatabaseName("IX_Alerts_OneActivePerKey")
            .HasFilter("\"ClearedAtUtc\" IS NULL");
        builder.HasIndex(alert => alert.RaisedAtUtc);

        builder.HasRowVersion();
    }
}
