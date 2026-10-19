namespace OxBox.Application.Abstractions;

/// <summary>Where to reach a box's BU.</summary>
public sealed record BoxEndpoint(int BoxId, string BoxCode, string Host, int Port);
