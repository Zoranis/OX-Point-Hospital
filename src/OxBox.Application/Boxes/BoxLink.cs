using OxBox.Application.Feedback;
using OxBox.Application.Service;

namespace OxBox.Application.Boxes;

/// <summary>A box as the workflows see it: which box, its hardware, how to talk to its user, and the open service visit.</summary>
public sealed class BoxLink(int boxId, string boxCode, BoxHardware hardware, BoxSignals signals)
{
    public int BoxId => boxId;

    public string BoxCode => boxCode;

    public BoxHardware Hardware => hardware;

    public BoxSignals Signals => signals;

    /// <summary>
    /// The technician's visit in progress, or <c>null</c>. Kept in memory only: a server restart
    /// ends the visit, and every movement it made is already recorded.
    /// </summary>
    public ServiceVisit? Visit { get; set; }
}
