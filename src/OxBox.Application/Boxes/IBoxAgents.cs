namespace OxBox.Application.Boxes;

/// <summary>Starts and restarts box agents while the server runs, for boxes added or changed in the web app.</summary>
public interface IBoxAgents
{
    /// <summary>Starts the agent of a box that has none yet, or restarts it so it picks up a changed BU address.</summary>
    Task StartOrRestartAsync(int boxId, CancellationToken cancellationToken);
}
