namespace OxBox.Domain.Cylinders;

public enum CylinderChangeReason
{
    /// <summary>A user put the empty cylinder in the box during an exchange.</summary>
    Returned,

    /// <summary>A user took the full cylinder during an exchange.</summary>
    Dispensed,

    /// <summary>A technician put a refilled cylinder in the box.</summary>
    Restocked,

    /// <summary>A technician took an empty cylinder out for refilling.</summary>
    SentToSupplier,
}
