namespace OxBox.Infrastructure.Reports;

/// <summary>Where one cost centre's active cylinders are right now.</summary>
public sealed record CylinderCountRow(
    string CostCentreCode,
    string CostCentreName,
    int FullAtCostCentre,
    int EmptyAtCostCentre,
    int FullInBox,
    int EmptyInBox,
    int WithSupplier)
{
    public int AtCostCentre => FullAtCostCentre + EmptyAtCostCentre;

    public int InBox => FullInBox + EmptyInBox;

    /// <summary>
    /// Full cylinders on the hospital campus (flag 9201): full, and in a box or at a department.
    /// This definition awaits the hospital's confirmation (open question #4).
    /// </summary>
    public int FullOnCampus => FullInBox + FullAtCostCentre;

    public int Total => AtCostCentre + InBox + WithSupplier;
}
