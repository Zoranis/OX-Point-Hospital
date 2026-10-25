using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OxBox.Domain.Alerts;
using OxBox.Domain.Boxes;
using OxBox.Domain.CostCentres;
using OxBox.Domain.Cylinders;
using OxBox.Domain.Settings;
using OxBox.Domain.Technicians;
using OxBox.Domain.Transactions;
using OxBox.Infrastructure.Identity;
using OxBox.Infrastructure.Persistence.Configurations;

namespace OxBox.Infrastructure.Persistence;

public sealed class OxBoxDbContext(DbContextOptions<OxBoxDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Box> Boxes => Set<Box>();

    public DbSet<Position> Positions => Set<Position>();

    public DbSet<CostCentre> CostCentres => Set<CostCentre>();

    public DbSet<Cylinder> Cylinders => Set<Cylinder>();

    public DbSet<CylinderStatusChange> CylinderStatusChanges => Set<CylinderStatusChange>();

    public DbSet<CylinderTransaction> CylinderTransactions => Set<CylinderTransaction>();

    public DbSet<Technician> Technicians => Set<Technician>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<AlertRecipient> AlertRecipients => Set<AlertRecipient>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(OxBoxDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BumpRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        BumpRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot sort or compare DateTimeOffset, so times are stored as UTC ticks.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();

        // Enums are stored by name so the database stays readable and reordering an enum is safe.
        configurationBuilder.Properties<CylinderStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<CylinderLocation>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<CylinderChangeReason>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<CylinderAction>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<AlertKind>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<MessageChannel>().HaveConversion<string>().HaveMaxLength(10);
    }

    /// <summary>
    /// Increments the row version of every modified entity that has one. The original value stays
    /// in the WHERE clause of the update, so a row someone else changed meanwhile is not
    /// overwritten.
    /// </summary>
    private void BumpRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified
                && entry.Metadata.FindProperty(RowVersionExtensions.PropertyName) is { } property)
            {
                var version = entry.Property(property);
                version.CurrentValue = (long)version.OriginalValue! + 1;
            }
        }
    }

    private sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
        value => value.UtcTicks,
        ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
}
