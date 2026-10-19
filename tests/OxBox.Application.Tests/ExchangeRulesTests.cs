using OxBox.Application.Exchange;
using OxBox.Domain.Boxes;
using OxBox.Domain.Cylinders;
using static OxBox.Application.Tests.TestData;

namespace OxBox.Application.Tests;

public class ExchangeRulesTests
{
    private readonly Box _box = BoxWithPositions(4);
    private readonly Domain.CostCentres.CostCentre _surgery = CostCentre("SURGERY");
    private readonly Domain.CostCentres.CostCentre _icu = CostCentre("ICU");

    [Fact]
    public void Decide_ValidReturn_PlansLowestFreeSlotAndOwnCostCentresCylinder()
    {
        var full = FullInBox(_surgery, _box.Positions[1]);
        var scanned = AtCostCentre(_surgery);

        var decision = ExchangeRules.Decide(scanned, Inventory(full));

        decision.Should().Be(new ExchangeApproved(scanned, _box.Positions[0], full, _box.Positions[1]));
    }

    [Fact]
    public void Decide_UnknownBarcode_IsRejected()
    {
        Reject(scanned: null, Inventory()).Should().Be(ExchangeError.UnknownBarcode);
    }

    [Fact]
    public void Decide_DeactivatedCylinder_IsRejectedAsUnknown()
    {
        var scanned = AtCostCentre(_surgery);
        scanned.Deactivate();

        Reject(scanned, Inventory(FullInBox(_surgery, _box.Positions[1]))).Should().Be(ExchangeError.UnknownBarcode);
    }

    [Fact]
    public void Decide_CylinderAlreadyInBox_IsRejectedAsAlreadyReturned()
    {
        var alreadyReturned = EmptyInBox(_surgery, _box.Positions[0]);

        Reject(alreadyReturned, Inventory(alreadyReturned, FullInBox(_surgery, _box.Positions[1])))
            .Should().Be(ExchangeError.AlreadyReturned);
    }

    [Fact]
    public void Decide_CylinderWithSupplier_IsRejected()
    {
        var withSupplier = new Cylinder("CYL-S", _surgery, CylinderStatus.Empty, CylinderLocation.WithSupplier).WithId(1);

        Reject(withSupplier, Inventory(FullInBox(_surgery, _box.Positions[1]))).Should().Be(ExchangeError.NotAtCostCentre);
    }

    [Fact]
    public void Decide_OnlyOtherCostCentresHaveFullCylinders_IsRejected()
    {
        var icuFull = FullInBox(_icu, _box.Positions[1]);

        Reject(AtCostCentre(_surgery), Inventory(icuFull)).Should().Be(ExchangeError.NoFullCylinderForCostCentre);
    }

    [Fact]
    public void Decide_EveryPositionOccupied_IsRejected()
    {
        var cylinders = _box.Positions.Select(position => FullInBox(_surgery, position)).ToArray();

        Reject(AtCostCentre(_surgery), Inventory(cylinders)).Should().Be(ExchangeError.NoFreePosition);
    }

    private BoxInventory Inventory(params Cylinder[] cylindersInBox) => new(_box, cylindersInBox);

    private static ExchangeError Reject(Cylinder? scanned, BoxInventory inventory) =>
        ExchangeRules.Decide(scanned, inventory).Should().BeOfType<ExchangeRejected>().Subject.Error;
}
