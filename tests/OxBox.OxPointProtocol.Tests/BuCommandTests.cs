using OxBox.OxPointProtocol.Commands;
using OxBox.OxPointProtocol.Messages;

namespace OxBox.OxPointProtocol.Tests;

public class BuCommandTests
{
    public static TheoryData<BuCommand, string> DocumentedCommands => new()
    {
        { new OpenLock(new LockAddress(9, 16)), """{"Open":[9,16]}""" },
        { new RequestLockStatus(9), """{"Request":9}""" },
        { new SendKeypadStatus(GoodRead: true), """{"Status":"GR"}""" },
        { new SendKeypadStatus(GoodRead: false), """{"Status":"BR"}""" },
        { new PulseRelay(ShieldRelay.Heating), """{"OpenRelay":0}""" },
        { new RequestDoorContact(), """{"RequestRelay":0}""" },
    };

    [Theory]
    [MemberData(nameof(DocumentedCommands))]
    public void ToJson_DocumentedCommand_MatchesDocument(BuCommand command, string expectedJson)
    {
        command.ToJson().Should().Be(expectedJson);
    }

    [Fact]
    public void IsAnsweredBy_OpenLock_MatchesOnlyReplyForSameLock()
    {
        var command = new OpenLock(new LockAddress(1, 5));

        command.IsAnsweredBy(new LockOpenReply(new LockAddress(1, 5), true, "OK")).Should().BeTrue();
        command.IsAnsweredBy(new LockOpenReply(new LockAddress(1, 6), true, "OK")).Should().BeFalse();
        command.IsAnsweredBy(new LockOpenAcknowledged(true, "OK")).Should().BeTrue();
        command.IsAnsweredBy(new LockStatusReply(1, new LockMask(0))).Should().BeFalse();
    }

    [Fact]
    public void IsAnsweredBy_RequestLockStatus_MatchesOnlySameBoard()
    {
        var command = new RequestLockStatus(1);

        command.IsAnsweredBy(new LockStatusReply(1, new LockMask(0))).Should().BeTrue();
        command.IsAnsweredBy(new LockStatusReply(0, new LockMask(0))).Should().BeFalse();
    }

    [Fact]
    public void ExpectsReply_KeypadStatus_IsFalse()
    {
        new SendKeypadStatus(GoodRead: true).ExpectsReply.Should().BeFalse();
    }
}
