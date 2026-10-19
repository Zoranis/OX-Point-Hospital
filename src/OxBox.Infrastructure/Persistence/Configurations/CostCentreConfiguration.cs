using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.CostCentres;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class CostCentreConfiguration : IEntityTypeConfiguration<CostCentre>
{
    public void Configure(EntityTypeBuilder<CostCentre> builder)
    {
        builder.Property(costCentre => costCentre.Code).HasMaxLength(32);
        builder.Property(costCentre => costCentre.Name).HasMaxLength(100);
        builder.HasIndex(costCentre => costCentre.Code).IsUnique();
    }
}
