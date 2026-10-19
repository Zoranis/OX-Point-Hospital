using OxBox.Application.Exchange;
using OxBox.Application.Service;

namespace OxBox.Application.Feedback;

/// <summary>What the box display should show. The display decides wording and language.</summary>
public abstract record KioskScreen
{
    /// <summary>
    /// Brief screens (an exchange or an error) give way to <see cref="ThenShow"/> after a few
    /// seconds, so the person at the box is told what to do next.
    /// </summary>
    public virtual bool IsTransient => false;

    /// <summary>What a transient screen gives way to. Usually the instructions for the next user.</summary>
    public virtual KioskScreen ThenShow => new ReadyScreen();
}

/// <summary>Waiting for a user: "Scan the barcode of your empty cylinder".</summary>
public sealed record ReadyScreen : KioskScreen;

/// <summary>The box cannot serve users: its BU is unreachable.</summary>
public sealed record OutOfServiceScreen : KioskScreen;

/// <summary>Both slots are unlocked: put the empty cylinder into one, take the full one from the other.</summary>
public sealed record ExchangeOpenScreen(int ReturnPositionNumber, int DispensePositionNumber) : KioskScreen
{
    public override bool IsTransient => true;
}

/// <summary>An exchange error. <c>ReturnPositionNumber</c> is set when the empty cylinder's slot did open.</summary>
public sealed record RejectedScreen(ExchangeError Error, int? ReturnPositionNumber = null) : KioskScreen
{
    public override bool IsTransient => true;
}

/// <summary>
/// Service visit: take the empty cylinders out of these slots, then scan each full cylinder.
/// <c>LastInsertedPositionNumber</c> is the slot opened for the cylinder scanned last, if any.
/// </summary>
public sealed record ServiceScreen(string TechnicianName, IReadOnlyList<int> EmptySlots, int RestockedCount, int? LastInsertedPositionNumber) : KioskScreen
{
    public bool Equals(ServiceScreen? other) =>
        other is not null
        && TechnicianName == other.TechnicianName
        && RestockedCount == other.RestockedCount
        && LastInsertedPositionNumber == other.LastInsertedPositionNumber
        && EmptySlots.SequenceEqual(other.EmptySlots);

    public override int GetHashCode() => HashCode.Combine(TechnicianName, RestockedCount, LastInsertedPositionNumber, EmptySlots.Count);
}

public sealed record ServiceRejectedScreen(ServiceError Error, KioskScreen Resume) : KioskScreen
{
    public override bool IsTransient => true;

    public override KioskScreen ThenShow => Resume;
}

/// <summary>Service visit over: a short summary before the box returns to normal use.</summary>
public sealed record ServiceFinishedScreen(int RemovedCount, int RestockedCount) : KioskScreen
{
    public override bool IsTransient => true;
}
