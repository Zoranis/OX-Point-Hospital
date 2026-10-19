using OxBox.OxPointProtocol.Messages;

namespace OxBox.OxPointProtocol;

/// <summary>
/// Every JSON key and fixed value of the OX Point protocol, defined once and shared by the
/// parsers and writers in both directions.
/// </summary>
internal static class WireNames
{
    // Messages from the BU.
    public const string Qr = "QR";
    public const string Rfid = "RFID";
    public const string Code = "CODE";
    public const string Response = "Response";
    public const string Board = "Board";
    public const string Lock = "Lock";
    public const string ResponseRelay = "ResponseRelay";
    public const string Info = "INFO";

    // Commands to the BU.
    public const string Open = "Open";
    public const string Request = "Request";
    public const string Status = "Status";
    public const string OpenRelay = "OpenRelay";
    public const string RequestRelay = "RequestRelay";

    // Fixed values.
    public const string Ok = "OK";
    public const string Ack = "ACK";
    public const string DoorClosed = "DOOR_CLOSED";
    public const string DoorOpened = "DOOR_OPENED";
    public const string GoodRead = "GR";
    public const string BadRead = "BR";

    public static readonly IReadOnlyDictionary<BuNotificationKind, string> Notifications =
        new Dictionary<BuNotificationKind, string>
        {
            [BuNotificationKind.OpenAllPressed] = "OPEN_ALL",
            [BuNotificationKind.DoorOpened] = DoorOpened,
            [BuNotificationKind.DoorClosed] = DoorClosed,
            [BuNotificationKind.WebSocketWasDisconnected] = "WS_DISCONNECT",
            [BuNotificationKind.EthernetWasDisconnected] = "ETH_DISCONNECT",
        };

    public static readonly IReadOnlyDictionary<string, BuNotificationKind> NotificationKinds =
        Notifications.ToDictionary(pair => pair.Value, pair => pair.Key);
}
