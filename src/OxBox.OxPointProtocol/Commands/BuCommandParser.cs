using System.Text.Json;

namespace OxBox.OxPointProtocol.Commands;

/// <summary>
/// Reads commands as the BU receives them. The server never needs this. It exists for the BU
/// simulator and tests, and round-trips with <see cref="BuCommand.ToJson"/>.
/// </summary>
public static class BuCommandParser
{
    /// <summary>Returns the command, or <c>null</c> if the JSON is not a documented command.</summary>
    public static BuCommand? Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object ? ParseObject(root) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static BuCommand? ParseObject(JsonElement root)
    {
        if (root.TryGetProperty(WireNames.Open, out var open))
        {
            return ParseOpen(open);
        }

        if (root.TryGetProperty(WireNames.Request, out var request))
        {
            return Json.TryGetInt(request, out var board) && OxPointLimits.IsValidBoard(board)
                ? new RequestLockStatus(board)
                : null;
        }

        if (root.TryGetProperty(WireNames.Status, out var status))
        {
            return Json.StringOf(status) switch
            {
                WireNames.GoodRead => new SendKeypadStatus(GoodRead: true),
                WireNames.BadRead => new SendKeypadStatus(GoodRead: false),
                _ => null,
            };
        }

        if (root.TryGetProperty(WireNames.OpenRelay, out var relay))
        {
            return Json.TryGetInt(relay, out var index) && Enum.IsDefined((ShieldRelay)index)
                ? new PulseRelay((ShieldRelay)index)
                : null;
        }

        // The value of RequestRelay is ignored by the BU.
        return root.TryGetProperty(WireNames.RequestRelay, out _) ? new RequestDoorContact() : null;
    }

    private static OpenLock? ParseOpen(JsonElement open)
    {
        if (open.ValueKind != JsonValueKind.Array || open.GetArrayLength() != 2)
        {
            return null;
        }

        return Json.TryGetInt(open[0], out var board) && OxPointLimits.IsValidBoard(board)
            && Json.TryGetInt(open[1], out var lockNumber) && OxPointLimits.IsValidLock(lockNumber)
                ? new OpenLock(new LockAddress(board, lockNumber))
                : null;
    }
}
