using OxBox.Domain.Boxes;
using OxBox.Domain.CostCentres;

namespace OxBox.Domain.Cylinders;

/// <summary>
/// An oxygen cylinder, identified for life by its barcode and owned by exactly one cost centre.
/// Its status and location are tracked here, not by the barcode.
/// </summary>
/// <remarks>
/// Every state change goes through an intent-named method that returns the matching
/// <see cref="CylinderStatusChange"/>, so callers cannot change state without an audit record.
/// The methods enforce invariants by throwing; workflows check the same conditions first and
/// turn them into user-facing errors.
/// </remarks>
public sealed class Cylinder
{
    private Cylinder()
    {
        Barcode = null!;
    }

    /// <summary>Registers a cylinder. It can only enter a box later, through a restocking scan.</summary>
    public Cylinder(string barcode, CostCentre costCentre, CylinderStatus status, CylinderLocation location)
    {
        if (location is not (CylinderLocation.AtCostCentre or CylinderLocation.WithSupplier))
        {
            throw new ArgumentException(
                "A new cylinder must start at its cost centre or with the supplier; it enters a box only by being scanned in.",
                nameof(location));
        }

        Barcode = Guard.NotBlank(barcode, nameof(barcode));
        CostCentre = costCentre;
        CostCentreId = costCentre.Id;
        Status = status;
        Location = location;
        IsActive = true;
    }

    public int Id { get; private set; }

    public string Barcode { get; private set; }

    public int CostCentreId { get; private set; }

    public CostCentre CostCentre { get; private set; } = null!;

    public CylinderStatus Status { get; private set; }

    public CylinderLocation Location { get; private set; }

    /// <summary>Set only while <see cref="Location"/> is <see cref="CylinderLocation.InBox"/>.</summary>
    public int? PositionId { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>A user put this (empty) cylinder into <paramref name="position"/> during an exchange.</summary>
    public CylinderStatusChange ReturnToBox(Position position, DateTimeOffset atUtc)
    {
        Require(Location == CylinderLocation.AtCostCentre, "Only a cylinder at its cost centre can be returned.");
        return ChangeTo(CylinderStatus.Empty, CylinderLocation.InBox, position.Id, CylinderChangeReason.Returned, atUtc);
    }

    /// <summary>A user took this full cylinder out of the box during an exchange.</summary>
    public CylinderStatusChange DispenseToCostCentre(DateTimeOffset atUtc)
    {
        Require(Location == CylinderLocation.InBox && Status == CylinderStatus.Full, "Only a full cylinder in a box can be dispensed.");
        return ChangeTo(CylinderStatus.Full, CylinderLocation.AtCostCentre, positionId: null, CylinderChangeReason.Dispensed, atUtc);
    }

    /// <summary>A technician took this empty cylinder out of the box for refilling.</summary>
    public CylinderStatusChange SendToSupplier(DateTimeOffset atUtc)
    {
        Require(Location == CylinderLocation.InBox && Status == CylinderStatus.Empty, "Only an empty cylinder in a box can be sent for refilling.");
        return ChangeTo(CylinderStatus.Empty, CylinderLocation.WithSupplier, positionId: null, CylinderChangeReason.SentToSupplier, atUtc);
    }

    /// <summary>A technician put this refilled cylinder into <paramref name="position"/>.</summary>
    public CylinderStatusChange RestockIntoBox(Position position, DateTimeOffset atUtc)
    {
        Require(Location != CylinderLocation.InBox, "A cylinder already in a box cannot be restocked.");
        return ChangeTo(CylinderStatus.Full, CylinderLocation.InBox, position.Id, CylinderChangeReason.Restocked, atUtc);
    }

    /// <summary>
    /// Permanently removes a damaged or lost cylinder from use. Its history is kept. A cylinder
    /// in a box must leave it first, so the box never holds a cylinder it cannot hand out.
    /// </summary>
    public void Deactivate()
    {
        Require(Location != CylinderLocation.InBox, "A cylinder in a box cannot be deactivated; it must leave the box first.");
        IsActive = false;
    }

    private CylinderStatusChange ChangeTo(
        CylinderStatus status,
        CylinderLocation location,
        int? positionId,
        CylinderChangeReason reason,
        DateTimeOffset atUtc)
    {
        Require(IsActive, "A deactivated cylinder cannot change state.");

        var change = new CylinderStatusChange(Id, atUtc, reason, Status, Location, status, location);
        Status = status;
        Location = location;
        PositionId = positionId;
        return change;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
