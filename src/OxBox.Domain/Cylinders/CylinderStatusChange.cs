namespace OxBox.Domain.Cylinders;

/// <summary>Append-only audit record of one change to a cylinder's status or location.</summary>
public sealed class CylinderStatusChange
{
    private CylinderStatusChange()
    {
    }

    internal CylinderStatusChange(
        int cylinderId,
        DateTimeOffset atUtc,
        CylinderChangeReason reason,
        CylinderStatus fromStatus,
        CylinderLocation fromLocation,
        CylinderStatus toStatus,
        CylinderLocation toLocation)
    {
        CylinderId = cylinderId;
        AtUtc = atUtc;
        Reason = reason;
        FromStatus = fromStatus;
        FromLocation = fromLocation;
        ToStatus = toStatus;
        ToLocation = toLocation;
    }

    public long Id { get; private set; }

    public int CylinderId { get; private set; }

    public DateTimeOffset AtUtc { get; private set; }

    public CylinderChangeReason Reason { get; private set; }

    public CylinderStatus FromStatus { get; private set; }

    public CylinderLocation FromLocation { get; private set; }

    public CylinderStatus ToStatus { get; private set; }

    public CylinderLocation ToLocation { get; private set; }
}
