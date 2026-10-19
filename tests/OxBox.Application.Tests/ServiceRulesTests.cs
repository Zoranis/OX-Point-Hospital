using OxBox.Application.Service;
using OxBox.Domain.Boxes;
using OxBox.Domain.Cylinders;

namespace OxBox.Application.Tests;

public class ServiceRulesTests
{
    private readonly Box _box = TestData.BoxWithPositions(4);

    [Fact]
    public void DecideInsert_RefilledCylinder_GetsTheLowestFreeSlot()
    {
        var costCentre = TestData.CostCentre("1101");
        var inventory = Inventory(TestData.FullInBox(costCentre, _box.Positions[0]));

        var decision = ServiceRules.DecideInsert(WithSupplier(costCentre), inventory);

        decision.Should().BeOfType<InsertApproved>().Which.Position.Number.Should().Be(2);
    }

    [Fact]
    public void DecideInsert_UnknownBarcode_IsRejected()
    {
        ServiceRules.DecideInsert(null, Inventory()).Should().Be(new InsertRejected(ServiceError.UnknownBarcode));
    }

    [Fact]
    public void DecideInsert_CylinderAtItsDepartment_IsRejected()
    {
        var atWard = TestData.AtCostCentre(TestData.CostCentre("1101"));

        ServiceRules.DecideInsert(atWard, Inventory()).Should().Be(new InsertRejected(ServiceError.AtCostCentre));
    }

    [Fact]
    public void DecideInsert_AlreadyInTheBox_IsRejected()
    {
        var inBox = TestData.FullInBox(TestData.CostCentre("1101"), _box.Positions[0]);

        ServiceRules.DecideInsert(inBox, Inventory(inBox)).Should().Be(new InsertRejected(ServiceError.AlreadyInBox));
    }

    [Fact]
    public void DecideInsert_EveryPositionOccupied_IsRejected()
    {
        var costCentre = TestData.CostCentre("1101");
        var inventory = Inventory([.. _box.Positions.Select(position => TestData.FullInBox(costCentre, position))]);

        ServiceRules.DecideInsert(WithSupplier(costCentre), inventory).Should().Be(new InsertRejected(ServiceError.BoxFull));
    }

    private static Cylinder WithSupplier(Domain.CostCentres.CostCentre costCentre)
    {
        var cylinder = TestData.EmptyInBox(costCentre, TestData.BoxWithPositions(1).Positions[0]);
        cylinder.SendToSupplier(TestData.Now);
        return cylinder;
    }

    private BoxInventory Inventory(params Cylinder[] cylinders) => new(_box, cylinders);
}
