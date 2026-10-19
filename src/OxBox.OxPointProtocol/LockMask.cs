namespace OxBox.OxPointProtocol;

/// <summary>
/// The lock states of one CU, as reported by <c>{"Response": n, "Board": b}</c>.
/// </summary>
/// <remarks>
/// Bit 0 is lock 1 and bit 15 is lock 16. A <b>cleared</b> bit means the lock is closed;
/// a set bit means open <i>or not connected</i>, and the two cannot be told apart.
/// This follows the document's own example: 44975 (0xAFAF) has closed locks 5, 7, 13 and 15.
/// Callers must therefore only ask about locks that are actually wired.
/// </remarks>
public readonly record struct LockMask(ushort Bits)
{
    /// <summary>The mask a BU reports when exactly these locks are closed; every other lock reads as open.</summary>
    public static LockMask WithClosedLocks(IEnumerable<int> closedLocks)
    {
        var bits = ushort.MaxValue;
        foreach (var lockNumber in closedLocks)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(lockNumber, OxPointLimits.MinLockNumber);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(lockNumber, OxPointLimits.MaxLockNumber);
            bits &= (ushort)~(1 << (lockNumber - 1));
        }

        return new LockMask(bits);
    }

    public bool IsClosed(int lockNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(lockNumber, OxPointLimits.MinLockNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lockNumber, OxPointLimits.MaxLockNumber);

        var bit = 1 << (lockNumber - 1);
        return (Bits & bit) == 0;
    }

    public IEnumerable<int> ClosedLocks() =>
        Enumerable.Range(OxPointLimits.MinLockNumber, OxPointLimits.MaxLockNumber).Where(IsClosed);
}
