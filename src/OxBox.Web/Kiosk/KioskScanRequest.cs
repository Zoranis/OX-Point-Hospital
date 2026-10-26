namespace OxBox.Web.Kiosk;

/// <summary>A code read by a barcode reader plugged into the PC that shows a box's display.</summary>
public sealed record KioskScanRequest(string Code);
