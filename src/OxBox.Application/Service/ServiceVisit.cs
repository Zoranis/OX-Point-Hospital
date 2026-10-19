namespace OxBox.Application.Service;

/// <summary>A technician's restocking visit at one box, from their service-code scan until it ends.</summary>
public sealed class ServiceVisit(int technicianId, string technicianName, IReadOnlyList<int> emptySlots, DateTimeOffset startedUtc)
{
    public int TechnicianId => technicianId;

    public string TechnicianName => technicianName;

    /// <summary>The slots unlocked at the start so the technician can take the empty cylinders out.</summary>
    public IReadOnlyList<int> EmptySlots => emptySlots;

    public int RestockedCount { get; private set; }

    public int? LastInsertedPositionNumber { get; private set; }

    public DateTimeOffset LastActivityUtc { get; private set; } = startedUtc;

    public void NoteActivity(DateTimeOffset nowUtc) => LastActivityUtc = nowUtc;

    public void Restocked(int positionNumber)
    {
        RestockedCount++;
        LastInsertedPositionNumber = positionNumber;
    }

    public bool IsInactive(DateTimeOffset nowUtc, TimeSpan timeout) => nowUtc - LastActivityUtc >= timeout;
}
