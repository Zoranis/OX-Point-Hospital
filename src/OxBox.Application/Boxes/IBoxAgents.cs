namespace OxBox.Application.Boxes;

/// <summary>Starts and restarts box agents while the server runs, for boxes added or changed in the web app.</summary>
public interface IBoxAgents
{
    /// <summary>Starts the agent of a box that has none yet, or restarts it so it picks up a changed BU address.</summary>
    Task StartOrRestartAsync(int boxId, CancellationToken cancellationToken);

    /// <summary>
    /// Hands a barcode to a box's agent as if its BU had read it, for a reader plugged into the
    /// server PC instead of the BU. Real locks open. The web app offers it only in development.
    /// </summary>
    /// <returns>False if this server runs no agent for the box.</returns>
    Task<bool> SimulateScanAsync(int boxId, string code, string userName, CancellationToken cancellationToken);
}
