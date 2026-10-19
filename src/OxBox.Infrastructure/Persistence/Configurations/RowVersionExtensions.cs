using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal static class RowVersionExtensions
{
    /// <summary>
    /// Adds optimistic concurrency using PostgreSQL's system column <c>xmin</c>, which changes on
    /// every update. Npgsql maps a <see cref="uint"/> row version to <c>xmin</c>, so no real
    /// column is added. It is a shadow property, which keeps the domain model free of it.
    /// </summary>
    public static void HasRowVersion<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class =>
        builder.Property<uint>("Version").IsRowVersion();
}
