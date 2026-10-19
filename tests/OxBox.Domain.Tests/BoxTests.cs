using OxBox.Domain.Boxes;
using OxBox.OxPointProtocol;

namespace OxBox.Domain.Tests;

public class BoxTests
{
    [Fact]
    public void CreateWithSequentialWiring_ThirtyPositions_UsesCu0ThenCu1()
    {
        var box = Box.CreateWithSequentialWiring("BOX-01", "Main box", "Building A", "10.0.0.5", 3030, positionCount: 30);

        box.Positions.Should().HaveCount(30);
        box.Positions[0].Lock.Should().Be(new LockAddress(0, 1));
        box.Positions[15].Lock.Should().Be(new LockAddress(0, 16));
        box.Positions[16].Lock.Should().Be(new LockAddress(1, 1));
        box.Positions[29].Lock.Should().Be(new LockAddress(1, 14));
        box.Positions.Select(position => position.Number).Should().BeEquivalentTo(Enumerable.Range(1, 30));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(161)]
    public void CreateWithSequentialWiring_PositionCountOutOfRange_Throws(int positionCount)
    {
        var act = () => Box.CreateWithSequentialWiring("BOX-01", "Box", "", "bu", 3030, positionCount);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
