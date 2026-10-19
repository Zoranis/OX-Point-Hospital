namespace OxBox.Domain.Alerts;

/// <summary>The stock levels the RFQ asks to be alerted on, per cost centre and box.</summary>
public enum AlertKind
{
    /// <summary>A cost centre is at or below its low-stock threshold of full cylinders in a box.</summary>
    LowStock,

    /// <summary>A cost centre has only one full cylinder left in a box (RFQ: restock now).</summary>
    LastFullCylinder,

    /// <summary>A cost centre has no full cylinder in a box: its oxygen supply is interrupted.</summary>
    NoFullCylinder,
}
