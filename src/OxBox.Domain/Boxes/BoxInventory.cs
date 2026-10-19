using OxBox.Domain.Cylinders;

namespace OxBox.Domain.Boxes;

/// <summary>
/// What is in one box right now: its positions and the cylinders in them. A box has only a few
/// dozen positions, so it is loaded whole and every decision is made in memory.
/// </summary>
public sealed class BoxInventory
{
    private readonly Dictionary<int, Position> _positionsById;
    private readonly Dictionary<int, Cylinder> _cylindersByPositionId;

    public BoxInventory(Box box, IEnumerable<Cylinder> cylindersInBox)
    {
        Box = box;
        _positionsById = box.Positions.ToDictionary(position => position.Id);
        _cylindersByPositionId = cylindersInBox
            .Where(cylinder => cylinder is { Location: CylinderLocation.InBox, PositionId: not null })
            .ToDictionary(cylinder => cylinder.PositionId!.Value);
    }

    public Box Box { get; }

    public IReadOnlyCollection<Cylinder> Cylinders => _cylindersByPositionId.Values;

    public Position GetPosition(int positionId) => _positionsById[positionId];

    public Cylinder? CylinderAt(Position position) => _cylindersByPositionId.GetValueOrDefault(position.Id);

    /// <summary>The lowest-numbered empty position, or <c>null</c>.</summary>
    public Position? FindFreePosition() => FreePositions().MinBy(position => position.Number);

    public IEnumerable<Position> FreePositions() =>
        Box.Positions.Where(position => !_cylindersByPositionId.ContainsKey(position.Id));

    /// <summary>
    /// A full cylinder of the given cost centre, or <c>null</c>. The lowest position number is
    /// chosen so that the choice is predictable for staff.
    /// </summary>
    public (Cylinder Cylinder, Position Position)? FindFullCylinderFor(int costCentreId) =>
        FullCylinders()
            .Where(entry => entry.Cylinder.CostCentreId == costCentreId)
            .OrderBy(entry => entry.Position.Number)
            .Select(entry => ((Cylinder, Position)?)entry)
            .FirstOrDefault();

    public IEnumerable<(Cylinder Cylinder, Position Position)> FullCylinders() =>
        _cylindersByPositionId
            .Select(entry => (Cylinder: entry.Value, Position: _positionsById[entry.Key]))
            .Where(entry => entry.Cylinder is { Status: CylinderStatus.Full, IsActive: true });

    /// <summary>Positions holding empty cylinders, which a technician takes out for refilling.</summary>
    public IEnumerable<Position> PositionsWithEmptyCylinders() =>
        _cylindersByPositionId
            .Where(entry => entry.Value.Status == CylinderStatus.Empty)
            .Select(entry => _positionsById[entry.Key])
            .OrderBy(position => position.Number);
}
