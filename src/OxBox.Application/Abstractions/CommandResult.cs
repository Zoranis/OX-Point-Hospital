using OxBox.OxPointProtocol.Messages;

namespace OxBox.Application.Abstractions;

public enum CommandOutcome
{
    /// <summary>The BU sent the matching reply. It is in <see cref="CommandResult.Reply"/>.</summary>
    Replied,

    /// <summary>The command was sent and, by protocol, gets no reply.</summary>
    SentWithoutReply,

    /// <summary>No matching reply arrived within the command timeout.</summary>
    TimedOut,

    /// <summary>The BU was not connected, or the connection dropped while waiting.</summary>
    NotConnected,
}

public sealed record CommandResult(CommandOutcome Outcome, BuMessage? Reply = null)
{
    public static CommandResult NotConnected { get; } = new(CommandOutcome.NotConnected);

    public static CommandResult TimedOut { get; } = new(CommandOutcome.TimedOut);

    public static CommandResult SentWithoutReply { get; } = new(CommandOutcome.SentWithoutReply);

    public static CommandResult Replied(BuMessage reply) => new(CommandOutcome.Replied, reply);
}
