using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.Technicians;
using OxBox.Domain.Transactions;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class CylinderTransactionConfiguration : IEntityTypeConfiguration<CylinderTransaction>
{
    public void Configure(EntityTypeBuilder<CylinderTransaction> builder)
    {
        builder.Property(transaction => transaction.BoxCode).HasMaxLength(32);
        builder.Property(transaction => transaction.Barcode).HasMaxLength(64);
        builder.Property(transaction => transaction.CostCentreCode).HasMaxLength(32);

        builder.HasOne<Technician>()
            .WithMany()
            .HasForeignKey(transaction => transaction.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        // Invoicing and history reports filter by period, then by cost centre or box.
        builder.HasIndex(transaction => new { transaction.AtUtc, transaction.Action });
        builder.HasIndex(transaction => new { transaction.CostCentreCode, transaction.AtUtc });
        builder.HasIndex(transaction => transaction.Barcode);
    }
}
