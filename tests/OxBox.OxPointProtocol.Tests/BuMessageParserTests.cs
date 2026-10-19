using OxBox.OxPointProtocol.Messages;

namespace OxBox.OxPointProtocol.Tests;

/// <summary>JSON samples are copied from the OX Point technical document, section 5.</summary>
public class BuMessageParserTests
{
    [Fact]
    public void Parse_CardScan_ReturnsRfidScan()
    {
        var message = BuMessageParser.Parse("""{ "RFID" : 123456 }""");

        message.Should().Be(new ScanReceived("123456", ScanSource.Rfid));
    }

    [Fact]
    public void Parse_KeypadCode_ReturnsKeypadScan()
    {
        var message = BuMessageParser.Parse("""{ "CODE" : 123456 }""");

        message.Should().Be(new ScanReceived("123456", ScanSource.Keypad));
    }

    [Fact]
    public void Parse_QrCode_ReturnsQrScan()
    {
        var message = BuMessageParser.Parse("""{ "QR" : "123code456" }""");

        message.Should().Be(new ScanReceived("123code456", ScanSource.QrOrBarcode));
    }

    [Fact]
    public void Parse_LongNumericCode_KeepsEveryDigit()
    {
        var message = BuMessageParser.Parse("""{ "CODE" : 123456789012345 }""");

        message.Should().Be(new ScanReceived("123456789012345", ScanSource.Keypad));
    }

    [Fact]
    public void Parse_LockOpenConfirmation_ReturnsSuccessfulOpenReply()
    {
        var message = BuMessageParser.Parse("""{ "Response" : "OK", "Board" : 9, "Lock" : 16 }""");

        message.Should().Be(new LockOpenReply(new LockAddress(9, 16), Succeeded: true, "OK"));
    }

    [Fact]
    public void Parse_LockOpenWithOtherResponse_ReturnsFailedOpenReply()
    {
        var message = BuMessageParser.Parse("""{ "Response" : "ERR", "Board" : 0, "Lock" : 1 }""");

        message.Should().BeOfType<LockOpenReply>().Which.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Parse_LockStatus_ReturnsMaskForBoard()
    {
        var message = BuMessageParser.Parse("""{ "Response" : 44975, "Board" : 9 }""");

        var reply = message.Should().BeOfType<LockStatusReply>().Subject;
        reply.Board.Should().Be(9);
        reply.Locks.Bits.Should().Be(44975);
    }

    [Theory]
    [InlineData("""{ "ResponseRelay" : "DOOR_CLOSED" }""", true)]
    [InlineData("""{ "ResponseRelay" : "DOOR_OPENED" }""", false)]
    public void Parse_DoorContactReply_ReturnsDoorState(string json, bool expectedClosed)
    {
        BuMessageParser.Parse(json).Should().Be(new DoorContactReply(expectedClosed));
    }

    [Fact]
    public void Parse_RelayAck_ReturnsRelayAcknowledged()
    {
        BuMessageParser.Parse("""{ "ResponseRelay" : "ACK" }""").Should().BeOfType<RelayAcknowledged>();
    }

    [Theory]
    [InlineData("OPEN_ALL", BuNotificationKind.OpenAllPressed)]
    [InlineData("DOOR_OPENED", BuNotificationKind.DoorOpened)]
    [InlineData("DOOR_CLOSED", BuNotificationKind.DoorClosed)]
    [InlineData("WS_DISCONNECT", BuNotificationKind.WebSocketWasDisconnected)]
    [InlineData("ETH_DISCONNECT", BuNotificationKind.EthernetWasDisconnected)]
    public void Parse_InfoNotification_ReturnsKind(string info, BuNotificationKind expected)
    {
        BuMessageParser.Parse($$"""{ "INFO" : "{{info}}" }""").Should().Be(new BuNotification(expected));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    [InlineData("""{ "Unknown" : 1 }""")]
    [InlineData("""{ "INFO" : "SOMETHING_NEW" }""")]
    [InlineData("""{ "Response" : "OK", "Board" : 10, "Lock" : 1 }""")]
    [InlineData("""{ "Response" : "OK", "Board" : 0, "Lock" : 17 }""")]
    [InlineData("""{ "Response" : 70000, "Board" : 0 }""")]
    [InlineData("""{ "Response" : "OK" }""")]
    [InlineData("""{ "QR" : "" }""")]
    public void Parse_UnexpectedInput_ReturnsUnrecognizedWithoutThrowing(string json)
    {
        BuMessageParser.Parse(json).Should().Be(new UnrecognizedMessage(json));
    }
}
