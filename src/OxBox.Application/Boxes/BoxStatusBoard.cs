using System.Collections.Concurrent;

namespace OxBox.Application.Boxes;

/// <summary>Whether this server runs a box right now, and whether it reaches the box's BU.</summary>
public sealed record BoxLiveStatus(bool IsRunHere, bool IsConnected, DateTimeOffset SinceUtc);

/// <summary>
/// The live connection state of every box, as the agents report it, for the dashboard. In memory
/// only: it describes this server process.
/// </summary>
public sealed class BoxStatusBoard(TimeProvider time)
{
    private readonly ConcurrentDictionary<int, BoxLiveStatus> _statuses = new();

    public BoxLiveStatus Get(int boxId) =>
        _statuses.GetValueOrDefault(boxId) ?? new BoxLiveStatus(IsRunHere: false, IsConnected: false, time.GetUtcNow());

    public void Report(int boxId, bool isRunHere, bool isConnected)
    {
        var current = _statuses.GetValueOrDefault(boxId);
        if (current is null || current.IsRunHere != isRunHere || current.IsConnected != isConnected)
        {
            _statuses[boxId] = new BoxLiveStatus(isRunHere, isConnected, time.GetUtcNow());
        }
    }
}
