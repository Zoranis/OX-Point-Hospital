using OxBox.Domain.Boxes;
using OxBox.Domain.Cylinders;

namespace OxBox.Application.Service;

/// <summary>Decides whether a scanned cylinder can be put into the box during a service visit. Pure.</summary>
public static class ServiceRules
{
    public static ServiceDecision DecideInsert(Cylinder? scanned, BoxInventory inventory)
    {
        if (scanned is not { IsActive: true })
        {
            return new InsertRejected(ServiceError.UnknownBarcode);
        }

        if (scanned.Location == CylinderLocation.InBox)
        {
            return new InsertRejected(ServiceError.AlreadyInBox);
        }

        if (scanned.Location == CylinderLocation.AtCostCentre)
        {
            return new InsertRejected(ServiceError.AtCostCentre);
        }

        if (inventory.FindFreePosition() is not { } slot)
        {
            return new InsertRejected(ServiceError.BoxFull);
        }

        return new InsertApproved(scanned, slot);
    }
}
