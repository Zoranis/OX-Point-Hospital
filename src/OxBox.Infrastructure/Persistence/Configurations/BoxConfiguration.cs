using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.Boxes;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class BoxConfiguration : IEntityTypeConfiguration<Box>
{
    public void Configure(EntityTypeBuilder<Box> builder)
    {
        builder.Property(box => box.Code).HasMaxLength(32);
        builder.Property(box => box.Name).HasMaxLength(100);
        builder.Property(box => box.Site).HasMaxLength(200);
        builder.Property(box => box.BuHost).HasMaxLength(253);
        builder.HasIndex(box => box.Code).IsUnique();

        builder.HasMany(box => box.Positions)
            .WithOne()
            .HasForeignKey(position => position.BoxId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(box => box.Positions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
