using OxBox.OxPointProtocol;

namespace OxBox.BuSimulator;

/// <summary>The endpoints behind the control page: what a person would do at the physical box.</summary>
public static class ControlApi
{
    public static void MapControlApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/state", (SimulatedBu bu, BuClients clients) => bu.Snapshot(clients.Count));

        api.MapPost("/scan", async (ScanRequest request, SimulatedBu bu, BuClients clients) =>
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Results.BadRequest("A code is required.");
            }

            await clients.BroadcastAsync(bu.Scan(request.Code));
            return Results.NoContent();
        });

        api.MapPost("/locks/{board:int}/{lockNumber:int}/close", (int board, int lockNumber, SimulatedBu bu) =>
            TryAddress(board, lockNumber) is { } address && bu.CloseDoor(address) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/locks/{board:int}/{lockNumber:int}/toggle-jam", (int board, int lockNumber, SimulatedBu bu) =>
            TryAddress(board, lockNumber) is { } address && bu.ToggleJam(address) ? Results.NoContent() : Results.NotFound());
    }

    private static LockAddress? TryAddress(int board, int lockNumber) =>
        OxPointLimits.IsValidBoard(board) && OxPointLimits.IsValidLock(lockNumber) ? new LockAddress(board, lockNumber) : null;

    public sealed record ScanRequest(string Code);
}
