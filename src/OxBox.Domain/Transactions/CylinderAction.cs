namespace OxBox.Domain.Transactions;

public enum CylinderAction
{
    /// <summary>A user returned an empty cylinder into the box.</summary>
    Return,

    /// <summary>A user took a full cylinder. This is the basis for invoicing.</summary>
    Dispense,

    /// <summary>A technician took an empty cylinder out for refilling.</summary>
    RestockOut,

    /// <summary>A technician put a refilled cylinder into the box.</summary>
    RestockIn,
}
