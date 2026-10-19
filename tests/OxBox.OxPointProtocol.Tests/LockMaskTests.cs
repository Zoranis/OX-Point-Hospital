namespace OxBox.OxPointProtocol.Tests;

public class LockMaskTests
{
    [Fact]
    public void ClosedLocks_DocumentExample_MatchesDocumentedLocks()
    {
        // OX Point document, section 5.8: 44975 = 1010'1111'1010'1111 → closed locks 15, 13, 7, 5.
        var mask = new LockMask(44975);

        mask.ClosedLocks().Should().BeEquivalentTo([5, 7, 13, 15]);
    }

    [Fact]
    public void IsClosed_ClearedBit_IsClosed()
    {
        var allOpenExceptLockOne = new LockMask(0b1111_1111_1111_1110);

        allOpenExceptLockOne.IsClosed(1).Should().BeTrue();
        allOpenExceptLockOne.IsClosed(2).Should().BeFalse();
    }

    [Fact]
    public void WithClosedLocks_DocumentExample_ProducesTheDocumentedValue()
    {
        LockMask.WithClosedLocks([5, 7, 13, 15]).Bits.Should().Be(44975);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public void IsClosed_LockOutOfRange_Throws(int lockNumber)
    {
        var act = () => new LockMask(0).IsClosed(lockNumber);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
