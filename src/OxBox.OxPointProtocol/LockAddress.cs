namespace OxBox.OxPointProtocol;

/// <summary>
/// One physical lock: the CU (board) it is wired to and its lock number on that board.
/// </summary>
public readonly record struct LockAddress
{
    public LockAddress(int board, int lockNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(board, OxPointLimits.MinBoardIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(board, OxPointLimits.MaxBoardIndex);
        ArgumentOutOfRangeException.ThrowIfLessThan(lockNumber, OxPointLimits.MinLockNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lockNumber, OxPointLimits.MaxLockNumber);

        Board = board;
        LockNumber = lockNumber;
    }

    public int Board { get; }

    public int LockNumber { get; }

    public override string ToString() => $"CU{Board}/L{LockNumber}";
}
