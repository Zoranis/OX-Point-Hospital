using OxBox.OxPointProtocol.Commands;
using OxBox.OxPointProtocol.Messages;

namespace OxBox.OxPointProtocol.Tests;

/// <summary>
/// The simulator writes messages and reads commands; the server does the opposite. These tests
/// guarantee both sides agree on every message.
/// </summary>
public class RoundTripTests
{
    public static TheoryData<BuMessage> Messages => new()
    {
        new ScanReceived("CYL-0042", ScanSource.QrOrBarcode),
        new ScanReceived("123456", ScanSource.Rfid),
        new ScanReceived("987654321012345", ScanSource.Keypad),
        new LockOpenReply(new LockAddress(9, 16), Succeeded: true, "OK"),
        new LockOpenAcknowledged(Succeeded: true, "OK"),
        new LockStatusReply(1, new LockMask(44975)),
        new RelayAcknowledged(),
        new DoorContactReply(IsClosed: true),
        new DoorContactReply(IsClosed: false),
        new BuNotification(BuNotificationKind.OpenAllPressed),
        new BuNotification(BuNotificationKind.DoorOpened),
        new BuNotification(BuNotificationKind.DoorClosed),
        new BuNotification(BuNotificationKind.WebSocketWasDisconnected),
        new BuNotification(BuNotificationKind.EthernetWasDisconnected),
    };

    public static TheoryData<BuCommand> Commands => new()
    {
        new OpenLock(new LockAddress(0, 1)),
        new OpenLock(new LockAddress(9, 16)),
        new RequestLockStatus(9),
        new SendKeypadStatus(GoodRead: true),
        new SendKeypadStatus(GoodRead: false),
        new PulseRelay(ShieldRelay.Heating),
        new PulseRelay(ShieldRelay.Cooling),
        new RequestDoorContact(),
    };

    [Theory]
    [MemberData(nameof(Messages))]
    public void Message_WrittenThenParsed_IsUnchanged(BuMessage message)
    {
        BuMessageParser.Parse(BuMessageWriter.ToJson(message)).Should().Be(message);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public void Command_WrittenThenParsed_IsUnchanged(BuCommand command)
    {
        BuCommandParser.Parse(command.ToJson()).Should().Be(command);
    }

    [Fact]
    public void Writer_NumericCardCode_IsWrittenAsJsonNumberLikeTheBu()
    {
        BuMessageWriter.ToJson(new ScanReceived("123456", ScanSource.Rfid)).Should().Be("""{"RFID":123456}""");
    }

    [Theory]
    [InlineData("""{ "Open" : [ 9, 16] }""")]
    [InlineData("""{ "Request" : 9 }""")]
    [InlineData("""{ "Status" : "GR" }""")]
    [InlineData("""{ "OpenRelay" : 0 }""")]
    [InlineData("""{ "RequestRelay" : 0 }""")]
    public void CommandParser_DocumentSamples_AreRecognised(string json)
    {
        BuCommandParser.Parse(json).Should().NotBeNull();
    }

    [Theory]
    [InlineData("""{ "Open" : [ 10, 1] }""")]
    [InlineData("""{ "Open" : [ 0 ] }""")]
    [InlineData("""{ "Request" : "x" }""")]
    [InlineData("""{ "Status" : "XX" }""")]
    [InlineData("""{ "OpenRelay" : 2 }""")]
    [InlineData("not json")]
    public void CommandParser_InvalidInput_ReturnsNull(string json)
    {
        BuCommandParser.Parse(json).Should().BeNull();
    }
}
