using OxBox.OxPointProtocol;

namespace OxBox.Domain.Boxes;

/// <summary>A dispensing box: one OX Point BU and its lockable positions.</summary>
public sealed class Box
{
    private readonly List<Position> _positions = [];

    private Box()
    {
        Code = null!;
        Name = null!;
        Site = null!;
        BuHost = null!;
    }

    private Box(string code, string name, string site, string buHost, int buPort)
    {
        Code = Guard.NotBlank(code, nameof(code));
        Name = Guard.NotBlank(name, nameof(name));
        Site = site.Trim();
        BuHost = Guard.NotBlank(buHost, nameof(buHost));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buPort);
        BuPort = buPort;
    }

    public int Id { get; private set; }

    /// <summary>The box ID recorded on every transaction.</summary>
    public string Code { get; private set; }

    public string Name { get; private set; }

    /// <summary>Where the box stands, as free text (building, floor).</summary>
    public string Site { get; private set; }

    public string BuHost { get; private set; }

    public int BuPort { get; private set; }

    public IReadOnlyList<Position> Positions => _positions;

    public void ChangeDetails(string name, string site)
    {
        Name = Guard.NotBlank(name, nameof(name));
        Site = site.Trim();
    }

    public void ChangeBuAddress(string buHost, int buPort)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buPort);
        BuHost = Guard.NotBlank(buHost, nameof(buHost));
        BuPort = buPort;
    }

    /// <summary>
    /// Creates a box whose positions are wired in order: positions 1–16 on CU0 locks 1–16,
    /// positions 17–32 on CU1, and so on. Individual positions can be rewired afterwards.
    /// </summary>
    public static Box CreateWithSequentialWiring(string code, string name, string site, string buHost, int buPort, int positionCount)
    {
        const int locksPerBoard = OxPointLimits.MaxLockNumber;
        const int maxPositions = (OxPointLimits.MaxBoardIndex + 1) * locksPerBoard;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(positionCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(positionCount, maxPositions);

        var box = new Box(code, name, site, buHost, buPort);
        for (var number = 1; number <= positionCount; number++)
        {
            var board = (number - 1) / locksPerBoard;
            var lockNumber = ((number - 1) % locksPerBoard) + 1;
            box._positions.Add(new Position(number, new LockAddress(board, lockNumber)));
        }

        return box;
    }
}
