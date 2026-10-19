namespace OxBox.Infrastructure.Reports;

/// <summary>
/// Narrows a report to one box and to cost centres. <c>AllowedCostCentres</c> is <c>null</c> for
/// every cost centre, otherwise a viewer's departments; <c>CostCentreCode</c> picks one of them.
/// </summary>
public sealed record ReportFilter(string? BoxCode, IReadOnlyCollection<string>? AllowedCostCentres, string? CostCentreCode = null);
