using System.Text.Json;

namespace OxBox.OxPointProtocol.Messages;

/// <summary>
/// Turns raw BU JSON into typed messages. It never throws: anything unexpected becomes
/// <see cref="UnrecognizedMessage"/>, so a single bad frame cannot stop the connection.
/// </summary>
public static class BuMessageParser
{
    private static readonly (string Key, ScanSource Source)[] ScanKeys =
    [
        (WireNames.Qr, ScanSource.QrOrBarcode),
        (WireNames.Rfid, ScanSource.Rfid),
        (WireNames.Code, ScanSource.Keypad),
    ];

    public static BuMessage Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var message = root.ValueKind == JsonValueKind.Object ? ParseObject(root) : null;
            return message ?? new UnrecognizedMessage(json);
        }
        catch (JsonException)
        {
            return new UnrecognizedMessage(json);
        }
    }

    private static BuMessage? ParseObject(JsonElement root)
    {
        foreach (var (key, source) in ScanKeys)
        {
            if (root.TryGetProperty(key, out var code))
            {
                return ParseScan(code, source);
            }
        }

        if (root.TryGetProperty(WireNames.Response, out var response))
        {
            return ParseResponse(root, response);
        }

        if (root.TryGetProperty(WireNames.ResponseRelay, out var relay))
        {
            return ParseRelayResponse(relay);
        }

        if (root.TryGetProperty(WireNames.Info, out var info))
        {
            return Json.StringOf(info) is { } name && WireNames.NotificationKinds.TryGetValue(name, out var kind)
                ? new BuNotification(kind)
                : null;
        }

        return null;
    }

    // RFID and CODE arrive as JSON numbers, QR as a string. The raw text of a number is
    // used so the code is never rounded or reformatted.
    private static ScanReceived? ParseScan(JsonElement code, ScanSource source) => code.ValueKind switch
    {
        JsonValueKind.String when !string.IsNullOrWhiteSpace(code.GetString()) => new ScanReceived(code.GetString()!, source),
        JsonValueKind.Number => new ScanReceived(code.GetRawText(), source),
        _ => null,
    };

    // "Response" means two different things: a string for a lock-open reply,
    // and a number (the lock bitmask) for a lock-status reply.
    private static BuMessage? ParseResponse(JsonElement root, JsonElement response)
    {
        // Firmware 13.IO2.01 answers an open with the response text alone.
        if (response.ValueKind == JsonValueKind.String
            && !root.TryGetProperty(WireNames.Board, out _)
            && !root.TryGetProperty(WireNames.Lock, out _))
        {
            var ack = response.GetString()!;
            return new LockOpenAcknowledged(Succeeded: ack == WireNames.Ok, ack);
        }

        if (!Json.TryGetInt(root, WireNames.Board, out var board) || !OxPointLimits.IsValidBoard(board))
        {
            return null;
        }

        if (response.ValueKind == JsonValueKind.Number)
        {
            return response.TryGetUInt16(out var bits) ? new LockStatusReply(board, new LockMask(bits)) : null;
        }

        if (response.ValueKind != JsonValueKind.String
            || !Json.TryGetInt(root, WireNames.Lock, out var lockNumber)
            || !OxPointLimits.IsValidLock(lockNumber))
        {
            return null;
        }

        var text = response.GetString()!;
        return new LockOpenReply(new LockAddress(board, lockNumber), Succeeded: text == WireNames.Ok, text);
    }

    private static BuMessage? ParseRelayResponse(JsonElement relay) => Json.StringOf(relay) switch
    {
        WireNames.Ack => new RelayAcknowledged(),
        WireNames.DoorClosed => new DoorContactReply(IsClosed: true),
        WireNames.DoorOpened => new DoorContactReply(IsClosed: false),
        _ => null,
    };
}
