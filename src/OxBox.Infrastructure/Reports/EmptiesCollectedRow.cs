namespace OxBox.Infrastructure.Reports;

/// <summary>
/// Empty cylinders collected in a period (flag 8441): those a cost centre returned at a box, and,
/// alongside, those a technician took out of a box for refilling.
/// </summary>
public sealed record EmptiesCollectedRow(string CostCentreCode, string CostCentreName, int Returned, int TakenForRefill);
