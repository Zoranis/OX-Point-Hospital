namespace OxBox.OxPointProtocol.Messages;

public enum BuNotificationKind
{
    /// <summary>The physical OPEN ALL button was held, and every lock behind the BU opened.</summary>
    OpenAllPressed,

    /// <summary>The TAMPER contact was disconnected.</summary>
    DoorOpened,

    /// <summary>The TAMPER contact was connected.</summary>
    DoorClosed,

    /// <summary>All WebSocket clients had been disconnected. Sent when one reconnects.</summary>
    WebSocketWasDisconnected,

    /// <summary>The BU lost Ethernet. Sent once Ethernet and a client are back.</summary>
    EthernetWasDisconnected,
}
