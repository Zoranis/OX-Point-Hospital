using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal static class RowVersionExtensions
{
    /// <summary>Name of the shadow property that holds the row version.</summary>
    public const string PropertyName = "Version";

    /// <summary>
    /// Adds optimistic concurrency with a <c>Version</c> column. SQLite has no row version of its
    /// own, so <see cref="OxBoxDbContext"/> increments it on every update. It is a shadow
    /// property, which keeps the domain model free of it.
    /// </summary>
    public static void HasRowVersion<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class =>
        builder.Property<long>(PropertyName).IsConcurrencyToken();
}
