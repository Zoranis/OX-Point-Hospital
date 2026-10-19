using OxBox.OxPointProtocol.Messages;

namespace OxBox.Application.Abstractions;

/// <summary>Something that happened on a box's BU connection.</summary>
public abstract record GatewayEvent;

/// <summary>A BU message that was not the reply to a pending command.</summary>
public sealed record BuMessageReceived(BuMessage Message) : GatewayEvent;

/// <summary>The WebSocket to the BU opened or closed.</summary>
public sealed record ConnectionChanged(bool IsConnected) : GatewayEvent;
