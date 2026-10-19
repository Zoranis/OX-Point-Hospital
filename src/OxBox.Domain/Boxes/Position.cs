using OxBox.OxPointProtocol;

namespace OxBox.Domain.Boxes;

/// <summary>
/// One lockable compartment in a box. Which cylinder occupies it is recorded on the cylinder
/// (<see cref="Cylinders.Cylinder.PositionId"/>), so occupancy has a single source of truth.
/// </summary>
public sealed class Position
{
    private Position()
    {
    }

    internal Position(int number, LockAddress lockAddress)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);
        Number = number;
        Board = lockAddress.Board;
        LockNumber = lockAddress.LockNumber;
    }

    public int Id { get; private set; }

    public int BoxId { get; private set; }

    /// <summary>The number shown to people, 1..n within the box.</summary>
    public int Number { get; private set; }

    public int Board { get; private set; }

    public int LockNumber { get; private set; }

    public LockAddress Lock => new(Board, LockNumber);
}
