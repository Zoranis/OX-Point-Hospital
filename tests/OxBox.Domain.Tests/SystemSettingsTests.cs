using OxBox.Domain.Settings;

namespace OxBox.Domain.Tests;

public class SystemSettingsTests
{
    [Fact]
    public void Defaults_MatchRfqFlags()
    {
        var settings = SystemSettings.Defaults();

        settings.FullFlag.Should().Be(9201);
        settings.EmptyFlag.Should().Be(8441);
    }

    [Fact]
    public void ChangeFlags_SameValueForBoth_Throws()
    {
        var act = () => SystemSettings.Defaults().ChangeFlags(1000, 1000);

        act.Should().Throw<ArgumentException>();
    }
}
