using System.Text.Json.Nodes;

namespace OxBox.OxPointProtocol.Messages;

/// <summary>
/// Writes BU messages as the BU sends them. The server never needs this. It exists for the BU
/// simulator and tests, and round-trips with <see cref="BuMessageParser"/>.
/// </summary>
public static class BuMessageWriter
{
    public static string ToJson(BuMessage message) => message switch
    {
        ScanReceived scan => Object(ScanKey(scan.Source), ScanValue(scan)),
        LockOpenReply reply => new JsonObject
        {
            [WireNames.Response] = reply.Response,
            [WireNames.Board] = reply.Lock.Board,
            [WireNames.Lock] = reply.Lock.LockNumber,
        }.ToJsonString(),
        LockOpenAcknowledged ack => Object(WireNames.Response, ack.Response),
        LockStatusReply status => new JsonObject
        {
            [WireNames.Response] = status.Locks.Bits,
            [WireNames.Board] = status.Board,
        }.ToJsonString(),
        RelayAcknowledged => Object(WireNames.ResponseRelay, WireNames.Ack),
        DoorContactReply door => Object(WireNames.ResponseRelay, door.IsClosed ? WireNames.DoorClosed : WireNames.DoorOpened),
        BuNotification notification => Object(WireNames.Info, WireNames.Notifications[notification.Kind]),
        UnrecognizedMessage unrecognized => unrecognized.Raw,
        _ => throw new ArgumentOutOfRangeException(nameof(message), message, "Unknown message type."),
    };

    private static string ScanKey(ScanSource source) => source switch
    {
        ScanSource.QrOrBarcode => WireNames.Qr,
        ScanSource.Rfid => WireNames.Rfid,
        ScanSource.Keypad => WireNames.Code,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown scan source."),
    };

    // The BU sends card and keypad codes as JSON numbers and QR codes as strings.
    private static JsonNode ScanValue(ScanReceived scan) =>
        scan.Source != ScanSource.QrOrBarcode && scan.Code.All(char.IsAsciiDigit)
            ? JsonNode.Parse(scan.Code)!
            : JsonValue.Create(scan.Code);

    private static string Object(string key, JsonNode value) => new JsonObject { [key] = value }.ToJsonString();
}
