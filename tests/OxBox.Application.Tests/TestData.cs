using System.Reflection;
using OxBox.Domain.Boxes;
using OxBox.Domain.CostCentres;
using OxBox.Domain.Cylinders;

namespace OxBox.Application.Tests;

/// <summary>
/// Builds domain objects with IDs, which the database normally assigns, so rules can be tested in memory.
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);

    private static int _nextId = 1000;

    public static T WithId<T>(this T entity, int id)
    {
        typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!.SetValue(entity, id);
        return entity;
    }

    public static Box BoxWithPositions(int count)
    {
        var box = Box.CreateWithSequentialWiring("BOX-01", "Box", "", "bu", 3030, count).WithId(1);
        foreach (var position in box.Positions)
        {
            position.WithId(position.Number);
        }

        return box;
    }

    public static CostCentre CostCentre(string code) => new CostCentre(code, code).WithId(NextId());

    public static Cylinder AtCostCentre(CostCentre costCentre) => NewCylinder(costCentre, CylinderStatus.Full, CylinderLocation.AtCostCentre);

    public static Cylinder FullInBox(CostCentre costCentre, Position position)
    {
        var cylinder = NewCylinder(costCentre, CylinderStatus.Empty, CylinderLocation.WithSupplier);
        cylinder.RestockIntoBox(position, Now);
        return cylinder;
    }

    public static Cylinder EmptyInBox(CostCentre costCentre, Position position)
    {
        var cylinder = AtCostCentre(costCentre);
        cylinder.ReturnToBox(position, Now);
        return cylinder;
    }

    private static Cylinder NewCylinder(CostCentre costCentre, CylinderStatus status, CylinderLocation location)
    {
        var id = NextId();
        return new Cylinder($"CYL-{id}", costCentre, status, location).WithId(id);
    }

    private static int NextId() => Interlocked.Increment(ref _nextId);
}
