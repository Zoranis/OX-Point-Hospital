using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.Technicians;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.Property(technician => technician.Name).HasMaxLength(100);
        builder.Property(technician => technician.ServiceBarcode).HasMaxLength(64);
        builder.HasIndex(technician => technician.ServiceBarcode).IsUnique();
    }
}
