namespace OxBox.Infrastructure.Reports;

/// <summary>Full cylinders handed out to one cost centre in a period: the basis for invoicing.</summary>
public sealed record DispensedRow(string CostCentreCode, string CostCentreName, int Dispensed);
