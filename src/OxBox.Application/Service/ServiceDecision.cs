using OxBox.Domain.Boxes;
using OxBox.Domain.Cylinders;

namespace OxBox.Application.Service;

/// <summary>The outcome of checking a cylinder scanned during a service visit.</summary>
public abstract record ServiceDecision;

public sealed record InsertApproved(Cylinder Cylinder, Position Position) : ServiceDecision;

public sealed record InsertRejected(ServiceError Error) : ServiceDecision;
