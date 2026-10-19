using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OxBox.Domain.Settings;

namespace OxBox.Infrastructure.Persistence.Configurations;

internal sealed class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.Property(settings => settings.Id).ValueGeneratedNever();
        builder.Property(settings => settings.DisplayTimeZoneId).HasMaxLength(64);
        builder.Ignore(settings => settings.DisplayTimeZone);

        // Exactly one settings row can exist.
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_SystemSettings_Singleton", $"\"Id\" = {SystemSettings.SingletonId}"));
    }
}
