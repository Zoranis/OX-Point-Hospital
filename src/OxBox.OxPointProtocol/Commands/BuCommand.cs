using System.Text.Json.Nodes;
using OxBox.OxPointProtocol.Messages;

namespace OxBox.OxPointProtocol.Commands;

/// <summary>
/// A command sent to a BU. Each command knows its JSON form and which reply answers it,
/// because BU replies carry no request ID.
/// </summary>
public abstract record BuCommand
{
    /// <summary>False for commands the BU never replies to.</summary>
    public virtual bool ExpectsReply => true;

    public abstract string ToJson();

    /// <summary>True when <paramref name="message"/> is the reply to this command.</summary>
    public abstract bool IsAnsweredBy(BuMessage message);
}

/// <summary><c>{"Open": [board, lock]}</c></summary>
public sealed record OpenLock(LockAddress Lock) : BuCommand
{
    public override string ToJson() =>
        new JsonObject { [WireNames.Open] = new JsonArray(Lock.Board, Lock.LockNumber) }.ToJsonString();

    public override bool IsAnsweredBy(BuMessage message) =>
        (message is LockOpenReply reply && reply.Lock == Lock) || message is LockOpenAcknowledged;
}

/// <summary><c>{"Request": board}</c></summary>
public sealed record RequestLockStatus(int Board) : BuCommand
{
    public override string ToJson() => new JsonObject { [WireNames.Request] = Board }.ToJsonString();

    public override bool IsAnsweredBy(BuMessage message) =>
        message is LockStatusReply reply && reply.Board == Board;
}

/// <summary>
/// <c>{"Status": "GR" | "BR"}</c>. The BU forwards it to an RS232 keypad, if one is fitted,
/// and sends no reply.
/// </summary>
public sealed record SendKeypadStatus(bool GoodRead) : BuCommand
{
    public override bool ExpectsReply => false;

    public override string ToJson() =>
        new JsonObject { [WireNames.Status] = GoodRead ? WireNames.GoodRead : WireNames.BadRead }.ToJsonString();

    public override bool IsAnsweredBy(BuMessage message) => false;
}

/// <summary>
/// <c>{"OpenRelay": 0 | 1}</c>. Pulses a relay on the optional shield for the time set
/// in the BU (2 s by default). No reply means the link dropped or no shield is fitted.
/// </summary>
public sealed record PulseRelay(ShieldRelay Relay) : BuCommand
{
    public override string ToJson() => new JsonObject { [WireNames.OpenRelay] = (int)Relay }.ToJsonString();

    public override bool IsAnsweredBy(BuMessage message) => message is RelayAcknowledged;
}

/// <summary><c>{"RequestRelay": 0}</c>. Reads the TAMPER door contact. The BU ignores the value.</summary>
public sealed record RequestDoorContact : BuCommand
{
    public override string ToJson() => new JsonObject { [WireNames.RequestRelay] = 0 }.ToJsonString();

    public override bool IsAnsweredBy(BuMessage message) => message is DoorContactReply;
}
