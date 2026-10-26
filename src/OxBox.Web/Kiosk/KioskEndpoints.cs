using System.Net;
using OxBox.Application.Abstractions;
using OxBox.Application.Boxes;

namespace OxBox.Web.Kiosk;

/// <summary>
/// Development only: lets a barcode reader plugged into this PC scan at a box's display, for a
/// demo without a reader on the BU. The scan is handled as if the BU had read it, so real locks
/// open.
/// </summary>
public static class KioskEndpoints
{
    // Longer than any barcode or QR text the box is meant to handle.
    private const int MaxCodeLength = 256;

    public static void MapKioskEndpoints(this WebApplication app)
    {
        // The display needs no sign-in, so this must never exist on an installed system.
        if (app.Environment.IsDevelopment())
        {
            app.MapPost("/kiosk/{boxCode}/scan", ScanAsync);
        }
    }

    /// <remarks>
    /// Only a browser on this PC may scan. The body must be JSON, which another website cannot
    /// send here without a CORS preflight that this server never allows.
    /// </remarks>
    private static async Task<IResult> ScanAsync(
        string boxCode,
        KioskScanRequest scan,
        HttpContext context,
        IBoxStore store,
        IBoxAgents agents,
        CancellationToken cancellationToken)
    {
        if (context.Connection.RemoteIpAddress is not { } address || !IPAddress.IsLoopback(address))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var code = scan.Code?.Trim() ?? string.Empty;
        if (code.Length is 0 or > MaxCodeLength)
        {
            return Results.BadRequest();
        }

        var boxes = await store.GetBoxEndpointsAsync(cancellationToken);
        var box = boxes.FirstOrDefault(b => string.Equals(b.BoxCode, boxCode, StringComparison.OrdinalIgnoreCase));
        if (box is null)
        {
            return Results.NotFound();
        }

        return await agents.SimulateScanAsync(box.BoxId, code, "the box display", cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }
}
