using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.Boxes;
using OxBox.Domain.Cylinders;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class CylinderConfiguration : IEntityTypeConfiguration<Cylinder>
{
    public void Configure(EntityTypeBuilder<Cylinder> builder)
    {
        builder.Property(cylinder => cylinder.Barcode).HasMaxLength(64);
        builder.HasIndex(cylinder => cylinder.Barcode).IsUnique();

        builder.HasOne(cylinder => cylinder.CostCentre)
            .WithMany()
            .HasForeignKey(cylinder => cylinder.CostCentreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Position>()
            .WithMany()
            .HasForeignKey(cylinder => cylinder.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        // The database enforces at most one cylinder per position.
        builder.HasIndex(cylinder => cylinder.PositionId)
            .IsUnique()
            .HasFilter("\"PositionId\" IS NOT NULL");

        // Stock queries filter by cost centre, status and location.
        builder.HasIndex(cylinder => new { cylinder.CostCentreId, cylinder.Status, cylinder.Location });

        builder.HasRowVersion();
    }
}
