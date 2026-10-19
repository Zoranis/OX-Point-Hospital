namespace OxBox.Domain.Transactions;

/// <summary>
/// Append-only record of one cylinder movement at a box. Values are copied in (barcode, cost
/// centre code, flag) so the record stays correct even if the referenced data changes later.
/// </summary>
public sealed class CylinderTransaction
{
    private CylinderTransaction()
    {
        BoxCode = null!;
        Barcode = null!;
        CostCentreCode = null!;
    }

    public CylinderTransaction(
        DateTimeOffset atUtc,
        CylinderAction action,
        string boxCode,
        int positionNumber,
        string barcode,
        string costCentreCode,
        int flagAtTime,
        int? technicianId = null)
    {
        AtUtc = atUtc;
        Action = action;
        BoxCode = Guard.NotBlank(boxCode, nameof(boxCode));
        PositionNumber = positionNumber;
        Barcode = Guard.NotBlank(barcode, nameof(barcode));
        CostCentreCode = Guard.NotBlank(costCentreCode, nameof(costCentreCode));
        FlagAtTime = flagAtTime;
        TechnicianId = technicianId;
    }

    public long Id { get; private set; }

    public DateTimeOffset AtUtc { get; private set; }

    public CylinderAction Action { get; private set; }

    public string BoxCode { get; private set; }

    public int PositionNumber { get; private set; }

    public string Barcode { get; private set; }

    public string CostCentreCode { get; private set; }

    /// <summary>
    /// The full/empty flag (9201/8441 by default) in force at the time. Kept for audit only:
    /// reports use <see cref="Action"/>, so changing the flags later never alters history.
    /// </summary>
    public int FlagAtTime { get; private set; }

    /// <summary>Set for restocking, which is done by a technician.</summary>
    public int? TechnicianId { get; private set; }
}
