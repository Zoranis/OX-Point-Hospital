namespace OxBox.Application.Exchange;

/// <summary>Why a scan did not lead to a complete exchange. Each one has its own message on the box display.</summary>
public enum ExchangeError
{
    /// <summary>The barcode is not registered, or the cylinder was deactivated (RFQ: must not unlock anything).</summary>
    UnknownBarcode,

    /// <summary>The cylinder is already recorded in a box (RFQ: repeated scan).</summary>
    AlreadyReturned,

    /// <summary>The cylinder is not recorded as being at its cost centre, e.g. it is still with the supplier.</summary>
    NotAtCostCentre,

    /// <summary>No full cylinder of the cylinder's cost centre is in the box (RFQ: alert immediately).</summary>
    NoFullCylinderForCostCentre,

    /// <summary>Every position is occupied, so the empty cylinder has nowhere to go.</summary>
    NoFreePosition,

    /// <summary>The return slot's lock did not open; nothing was recorded.</summary>
    HardwareFault,

    /// <summary>The empty cylinder was taken back, but the full cylinder's lock did not open.</summary>
    DispenseFailed,
}
