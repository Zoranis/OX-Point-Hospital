using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OxBox.Domain.Alerts;
using OxBox.Domain.CostCentres;
using OxBox.Domain.Cylinders;
using OxBox.Domain.Technicians;
using OxBox.Domain.Transactions;

namespace OxBox.Infrastructure.Persistence;

/// <summary>
/// Fills an empty database with a small, realistic data set so the box can be demonstrated with
/// the BU simulator. Its barcodes match the simulator's one-click buttons.
/// </summary>
/// <remarks>
/// Three cost centres. Internal medicine and surgery each have 3 full cylinders in the box and 3
/// out on the ward. Anaesthesiology (ARO) has only 1 full cylinder in the box, so its second
/// return shows the "no full cylinder" state; two refilled ARO cylinders (O2-ARO-04, -05) wait at
/// the supplier for a technician to restock. Technician Jan Novák has service code SERVIS-001.
/// </remarks>
public sealed partial class DemoDataSeeder(OxBoxDbContext db, TimeProvider time, ILogger<DemoDataSeeder> logger)
{
    private static readonly DemoCostCentre[] CostCentres =
    [
        new("1101", "Interní klinika", "O2-INT", FullInBox: 3, OnWard: 3, WithSupplier: 0),
        new("1202", "Chirurgická klinika", "O2-CHI", FullInBox: 3, OnWard: 3, WithSupplier: 0),
        new("1305", "ARO", "O2-ARO", FullInBox: 1, OnWard: 2, WithSupplier: 2),
    ];

    /// <summary>
    /// Seeds an empty database. <paramref name="boxBarcodes"/> lists the demo cylinders to place
    /// full in the box, slot 1 first; empty for the standard layout.
    /// </summary>
    public async Task SeedAsync(IReadOnlyList<string> boxBarcodes, CancellationToken cancellationToken)
    {
        if (await db.CostCentres.AnyAsync(cancellationToken)
            || await db.Boxes.Include(b => b.Positions).OrderBy(b => b.Id).FirstOrDefaultAsync(cancellationToken) is not { } box)
        {
            return;
        }

        var settings = await db.SystemSettings.SingleAsync(cancellationToken);
        var standardInBox = new List<Cylinder>();

        foreach (var demo in CostCentres)
        {
            var costCentre = new CostCentre(demo.Code, demo.Name);
            db.CostCentres.Add(costCentre);

            var number = 1;
            for (var i = 0; i < demo.FullInBox; i++)
            {
                var cylinder = new Cylinder(Barcode(demo, number++), costCentre, CylinderStatus.Empty, CylinderLocation.WithSupplier);
                db.Cylinders.Add(cylinder);
                standardInBox.Add(cylinder);
            }

            for (var i = 0; i < demo.OnWard; i++)
            {
                db.Cylinders.Add(new Cylinder(Barcode(demo, number++), costCentre, CylinderStatus.Full, CylinderLocation.AtCostCentre));
            }

            // Refilled and waiting at the supplier: a technician can restock these straight away.
            for (var i = 0; i < demo.WithSupplier; i++)
            {
                db.Cylinders.Add(new Cylinder(Barcode(demo, number++), costCentre, CylinderStatus.Full, CylinderLocation.WithSupplier));
            }
        }

        db.Technicians.Add(new Technician("Jan Novák", "SERVIS-001"));
        db.AlertRecipients.Add(new AlertRecipient("Údržba (maintenance)", "udrzba@nemocnice.test", "+420 600 000 000", costCentreId: null));

        // Cylinders need their IDs before their status changes can refer to them.
        await db.SaveChangesAsync(cancellationToken);

        var toRestock = boxBarcodes.Count == 0 ? standardInBox : PickDemoCylinders(boxBarcodes);

        var now = time.GetUtcNow();
        foreach (var (position, cylinder) in box.Positions.OrderBy(position => position.Number).Zip(toRestock))
        {
            db.CylinderStatusChanges.Add(cylinder.RestockIntoBox(position, now));
            db.CylinderTransactions.Add(new CylinderTransaction(
                now, CylinderAction.RestockIn, box.Code, position.Number, cylinder.Barcode, cylinder.CostCentre.Code, settings.FullFlag));
        }

        await db.SaveChangesAsync(cancellationToken);
        LogSeeded(CostCentres.Length, toRestock.Count);
    }

    private List<Cylinder> PickDemoCylinders(IReadOnlyList<string> barcodes)
    {
        var cylinders = db.Cylinders.Local.ToDictionary(cylinder => cylinder.Barcode, StringComparer.Ordinal);
        return
        [
            .. barcodes.Select(barcode => cylinders.GetValueOrDefault(barcode.Trim())
                ?? throw new InvalidOperationException($"Database:DemoBoxBarcodes lists {barcode}, which is not a demo cylinder.")),
        ];
    }

    private static string Barcode(DemoCostCentre demo, int number) => $"{demo.BarcodePrefix}-{number:00}";

    private sealed record DemoCostCentre(string Code, string Name, string BarcodePrefix, int FullInBox, int OnWard, int WithSupplier);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo data: {CostCentres} cost centres, {InBox} full cylinders in the box")]
    private partial void LogSeeded(int costCentres, int inBox);
}
