using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.Boxes;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.Ignore(position => position.Lock);

        builder.HasIndex(position => new { position.BoxId, position.Number }).IsUnique();
        builder.HasIndex(position => new { position.BoxId, position.Board, position.LockNumber }).IsUnique();

        builder.HasRowVersion();
    }
}
