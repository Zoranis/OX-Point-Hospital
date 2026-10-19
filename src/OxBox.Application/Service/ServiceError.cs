namespace OxBox.Application.Service;

/// <summary>Why a scan during a service visit did nothing. Each one has its own message on the box display.</summary>
public enum ServiceError
{
    /// <summary>The barcode is not registered, or the cylinder was deactivated; nothing opens, as in normal use.</summary>
    UnknownBarcode,

    /// <summary>The cylinder is already recorded in a box.</summary>
    AlreadyInBox,

    /// <summary>
    /// The cylinder is recorded at its department. Restocking it would turn a possibly empty
    /// cylinder into a "full" one, so it must come back through a normal exchange.
    /// </summary>
    AtCostCentre,

    /// <summary>Every position is occupied.</summary>
    BoxFull,

    /// <summary>The free slot's lock did not open; the cylinder was not recorded.</summary>
    NoSlotOpened,
}
