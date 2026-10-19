using OxBox.Domain.Boxes;
using OxBox.Domain.Cylinders;

namespace OxBox.Application.Exchange;

/// <summary>The outcome of checking a scan: either a plan for the exchange, or the reason there is none.</summary>
public abstract record ExchangeDecision;

public sealed record ExchangeApproved(
    Cylinder ReturnCylinder,
    Position ReturnPosition,
    Cylinder DispenseCylinder,
    Position DispensePosition) : ExchangeDecision;

public sealed record ExchangeRejected(ExchangeError Error) : ExchangeDecision;
