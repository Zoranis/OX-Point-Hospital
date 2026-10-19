using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.Cylinders;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class CylinderStatusChangeConfiguration : IEntityTypeConfiguration<CylinderStatusChange>
{
    public void Configure(EntityTypeBuilder<CylinderStatusChange> builder)
    {
        builder.HasOne<Cylinder>()
            .WithMany()
            .HasForeignKey(change => change.CylinderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(change => new { change.CylinderId, change.AtUtc });
    }
}
