namespace OxBox.Domain.Cylinders;

public enum CylinderLocation
{
    /// <summary>In a box position; <see cref="Cylinder.PositionId"/> says which.</summary>
    InBox,

    /// <summary>Handed out to its cost centre.</summary>
    AtCostCentre,

    /// <summary>Taken out of the box by a technician for refilling.</summary>
    WithSupplier,
}
