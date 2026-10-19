using OxBox.Domain.Boxes;
using OxBox.Domain.CostCentres;
using OxBox.Domain.Cylinders;

namespace OxBox.Domain.Tests;

public class CylinderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);
    private static readonly CostCentre Surgery = new("1001", "Surgery");
    private static readonly Position AnyPosition = Box.CreateWithSequentialWiring("BOX-01", "Box", "", "bu", 3030, 1).Positions[0];

    [Fact]
    public void Constructor_InBox_Throws()
    {
        var act = () => new Cylinder("CYL-1", Surgery, CylinderStatus.Full, CylinderLocation.InBox);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_TrimsBarcodeAndIsActive()
    {
        var cylinder = new Cylinder("  CYL-1 ", Surgery, CylinderStatus.Empty, CylinderLocation.AtCostCentre);

        cylinder.Barcode.Should().Be("CYL-1");
        cylinder.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ReturnToBox_CylinderAtCostCentre_BecomesEmptyInBoxAndRecordsChange()
    {
        var cylinder = new Cylinder("CYL-1", Surgery, CylinderStatus.Full, CylinderLocation.AtCostCentre);

        var change = cylinder.ReturnToBox(AnyPosition, Now);

        cylinder.Status.Should().Be(CylinderStatus.Empty);
        cylinder.Location.Should().Be(CylinderLocation.InBox);
        change.Reason.Should().Be(CylinderChangeReason.Returned);
        change.FromLocation.Should().Be(CylinderLocation.AtCostCentre);
        change.ToLocation.Should().Be(CylinderLocation.InBox);
        change.AtUtc.Should().Be(Now);
    }

    [Fact]
    public void ReturnToBox_CylinderWithSupplier_Throws()
    {
        var cylinder = new Cylinder("CYL-1", Surgery, CylinderStatus.Empty, CylinderLocation.WithSupplier);

        var act = () => cylinder.ReturnToBox(AnyPosition, Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FullLifecycle_RestockDispenseReturnSendToSupplier_EndsWithSupplier()
    {
        var cylinder = new Cylinder("CYL-1", Surgery, CylinderStatus.Empty, CylinderLocation.WithSupplier);

        cylinder.RestockIntoBox(AnyPosition, Now);
        cylinder.DispenseToCostCentre(Now);
        cylinder.ReturnToBox(AnyPosition, Now);
        var change = cylinder.SendToSupplier(Now);

        cylinder.Status.Should().Be(CylinderStatus.Empty);
        cylinder.Location.Should().Be(CylinderLocation.WithSupplier);
        cylinder.PositionId.Should().BeNull();
        change.Reason.Should().Be(CylinderChangeReason.SentToSupplier);
    }

    [Fact]
    public void DispenseToCostCentre_EmptyCylinderInBox_Throws()
    {
        var cylinder = new Cylinder("CYL-1", Surgery, CylinderStatus.Full, CylinderLocation.AtCostCentre);
        cylinder.ReturnToBox(AnyPosition, Now);

        var act = () => cylinder.DispenseToCostCentre(Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AnyTransition_DeactivatedCylinder_Throws()
    {
        var cylinder = new Cylinder("CYL-1", Surgery, CylinderStatus.Full, CylinderLocation.AtCostCentre);
        cylinder.Deactivate();

        var act = () => cylinder.ReturnToBox(AnyPosition, Now);

        act.Should().Throw<InvalidOperationException>();
    }
}
