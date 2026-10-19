using OxBox.Domain.Boxes;
using OxBox.Domain.Cylinders;

namespace OxBox.Application.Exchange;

/// <summary>
/// Decides whether a scanned cylinder can be exchanged, in the order the RFQ lists the checks.
/// Pure: it reads the inventory and changes nothing.
/// </summary>
public static class ExchangeRules
{
    public static ExchangeDecision Decide(Cylinder? scanned, BoxInventory inventory)
    {
        if (scanned is not { IsActive: true })
        {
            return new ExchangeRejected(ExchangeError.UnknownBarcode);
        }

        if (scanned.Location == CylinderLocation.InBox)
        {
            return new ExchangeRejected(ExchangeError.AlreadyReturned);
        }

        if (scanned.Location != CylinderLocation.AtCostCentre)
        {
            return new ExchangeRejected(ExchangeError.NotAtCostCentre);
        }

        if (inventory.FindFullCylinderFor(scanned.CostCentreId) is not { } full)
        {
            return new ExchangeRejected(ExchangeError.NoFullCylinderForCostCentre);
        }

        if (inventory.FindFreePosition() is not { } free)
        {
            return new ExchangeRejected(ExchangeError.NoFreePosition);
        }

        return new ExchangeApproved(scanned, free, full.Cylinder, full.Position);
    }
}
