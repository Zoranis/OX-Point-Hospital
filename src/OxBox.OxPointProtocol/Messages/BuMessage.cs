namespace OxBox.OxPointProtocol.Messages;

/// <summary>
/// A message received from a BU: either a reply to a command or an unsolicited notification.
/// </summary>
public abstract record BuMessage;

/// <summary>A code read by any reader on the BU (card, keypad or QR/barcode).</summary>
public sealed record ScanReceived(string Code, ScanSource Source) : BuMessage;

/// <summary>Reply to <c>Open</c>: <c>{"Response": "OK", "Board": 9, "Lock": 16}</c>.</summary>
public sealed record LockOpenReply(LockAddress Lock, bool Succeeded, string Response) : BuMessage;

/// <summary>Reply to <c>Request</c>: <c>{"Response": 44975, "Board": 9}</c>.</summary>
public sealed record LockStatusReply(int Board, LockMask Locks) : BuMessage;

/// <summary>Reply to <c>OpenRelay</c>: <c>{"ResponseRelay": "ACK"}</c>.</summary>
public sealed record RelayAcknowledged : BuMessage;

/// <summary>Reply to <c>RequestRelay</c>: <c>{"ResponseRelay": "DOOR_CLOSED" | "DOOR_OPENED"}</c>.</summary>
public sealed record DoorContactReply(bool IsClosed) : BuMessage;

/// <summary>An unsolicited <c>{"INFO": ...}</c> notification.</summary>
public sealed record BuNotification(BuNotificationKind Kind) : BuMessage;

/// <summary>Anything that does not match the documented protocol, kept raw for logging.</summary>
public sealed record UnrecognizedMessage(string Raw) : BuMessage;
